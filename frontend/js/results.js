// results.js — Results/itinerary screen: summary, timeline, map, sharing

/**
 * Convert backend itinerary format to the internal format used by renderTimeline/renderResultsMap.
 * Backend returns: {plan: [{day, date, attractions: [{id, name, suggestedTime, durationMinutes, notes}]}]}
 * Internal format: {days: [{dayNumber, attractions: [{id, name, time, duration, lat, lon, notes}]}]}
 */
function normalizeItinerary(data) {
  if (!data) return null;
  if (data.days) return data; // already MOCK_ITINERARY format

  var rawDays = data.plan || [];

  var coordMap = {};
  state.attractions.forEach(function (a) {
    coordMap[String(a.id)] = { lat: a.lat || 0, lon: a.lon || 0 };
  });

  var days = rawDays.map(function (day) {
    var attrs = (day.attractions || []).map(function (attr) {
      var coords = coordMap[String(attr.id)] || {};
      return {
        id: attr.id,
        name: attr.name || '',
        time: attr.suggestedTime || attr.time || '',
        duration: attr.durationMinutes || attr.duration || 0,
        notes: attr.notes || '',
        lat: coords.lat || 0,
        lon: coords.lon || 0,
      };
    });
    return {
      dayNumber: day.day || day.dayNumber || 1,
      attractions: attrs,
    };
  });

  return { days: days };
}

/**
 * Navigate to results screen and load itinerary data.
 */
function showResults() {
  navigateTo('results');

  window.api.getItinerary(state.sessionId)
    .then(function (data) {
      updateState({ itinerary: normalizeItinerary(data) });
      renderResults();
    })
    .catch(function () {
      updateState({ itinerary: MOCK_ITINERARY });
      renderResults();
    });
}

/**
 * Render all results sub-panels.
 */
function renderResults() {
  renderVoteSummary();
  renderTimeline();
  renderResultsMap();
  bindShareButton();
}

/**
 * Render the vote summary grid showing each attraction's vote.
 */
function renderVoteSummary() {
  var grid = document.getElementById('results-vote-grid');
  if (!grid) return;

  grid.innerHTML = '';

  state.attractions.forEach(function (a) {
    var voted = state.votes[a.id];
    var card = document.createElement('div');
    card.className = 'vote-result-card';

    var info = document.createElement('div');
    var nameEl = document.createElement('div');
    nameEl.style.fontWeight = '600';
    nameEl.style.marginBottom = '4px';
    nameEl.textContent = a.name;

    var votesEl = document.createElement('div');
    votesEl.className = 'vote-result-card__votes';

    if (voted === true) {
      votesEl.innerHTML =
        '<span class="vote-result-card__yes">' +
        window.i18n.t('results.yes', state.language) + '</span>';
    } else if (voted === false) {
      votesEl.innerHTML =
        '<span class="vote-result-card__no">' +
        window.i18n.t('results.no', state.language) + '</span>';
    } else {
      votesEl.innerHTML = '<span style="color:var(--muted)">—</span>';
    }

    info.appendChild(nameEl);
    info.appendChild(votesEl);
    card.appendChild(info);
    grid.appendChild(card);
  });
}

/**
 * Render the day-by-day timeline from the itinerary.
 */
function renderTimeline() {
  var container = document.getElementById('results-timeline');
  if (!container || !state.itinerary) return;

  container.innerHTML = '';

  state.itinerary.days.forEach(function (day, idx) {
    var daySection = document.createElement('div');
    daySection.className = 'timeline__day animate-fade-in-up';
    daySection.style.animationDelay = (idx * 120) + 'ms';

    var header = document.createElement('div');
    header.className = 'timeline__day-header';

    var badge = document.createElement('div');
    badge.className = 'timeline__day-badge';
    badge.style.background = window.mapUtils.getDayColor(idx);
    badge.textContent = day.dayNumber;

    var label = document.createElement('div');
    label.className = 'timeline__day-label';
    label.textContent = window.i18n.t('results.day', state.language) + ' ' + day.dayNumber;

    header.appendChild(badge);
    header.appendChild(label);
    daySection.appendChild(header);

    var items = document.createElement('div');
    items.className = 'timeline__items';

    day.attractions.forEach(function (attr) {
      var item = document.createElement('div');
      item.className = 'timeline__item';

      var nameEl = document.createElement('div');
      nameEl.className = 'timeline__item-name';
      nameEl.textContent = attr.name;

      var meta = document.createElement('div');
      meta.className = 'timeline__item-meta';
      meta.textContent = attr.time + ' • ' + attr.duration +
        ' ' + window.i18n.t('results.minutes', state.language);
      if (attr.notes) {
        meta.textContent += ' • ' + attr.notes;
      }

      item.appendChild(nameEl);
      item.appendChild(meta);
      items.appendChild(item);
    });

    daySection.appendChild(items);
    container.appendChild(daySection);
  });
}

/**
 * Render the full results map with day-colored markers and routes.
 */
function renderResultsMap() {
  if (!state.itinerary) return;

  var mapContainer = document.getElementById('results-map');
  if (!mapContainer) return;

  var allPoints = [];
  state.itinerary.days.forEach(function (day, dayIdx) {
    day.attractions.forEach(function (attr) {
      allPoints.push({
        lat: attr.lat,
        lon: attr.lon,
        name: attr.name,
        dayIndex: dayIdx,
        dayNumber: day.dayNumber,
      });
    });
  });

  if (allPoints.length === 0) return;

  var center = allPoints[0];
  mapContainer.innerHTML = '';
  var map = window.mapUtils.initMap('results-map', center.lat, center.lon, 13);
  if (!map) return;
  map.invalidateSize();

  state.itinerary.days.forEach(function (day, dayIdx) {
    var color = window.mapUtils.getDayColor(dayIdx);
    var dayPoints = [];

    day.attractions.forEach(function (attr, attrIdx) {
      window.mapUtils.addMarker(
        map, attr.lat, attr.lon,
        '<strong>' + attr.name + '</strong><br>Day ' + day.dayNumber,
        color,
        (attrIdx + 1).toString()
      );
      dayPoints.push({ lat: attr.lat, lon: attr.lon });
    });

    if (dayPoints.length > 1) {
      window.mapUtils.drawRoute(map, dayPoints, color);
    }
  });

  window.mapUtils.fitBounds(map, allPoints);
}

/**
 * Bind the share/copy plan button.
 */
function bindShareButton() {
  var shareBtn = document.getElementById('results-share-btn');
  if (!shareBtn) return;

  shareBtn.addEventListener('click', function () {
    var text = generatePlanText();
    window.utils.copyToClipboard(text).then(function () {
      showToast(window.i18n.t('results.copied', state.language));
    });
  });
}

/**
 * Generate a plain-text version of the itinerary for clipboard.
 * @returns {string}
 */
function generatePlanText() {
  if (!state.itinerary) return '';

  var lines = ['AlignRoute — ' + state.destination, ''];

  state.itinerary.days.forEach(function (day) {
    lines.push('--- Day ' + day.dayNumber + ' ---');
    day.attractions.forEach(function (attr) {
      var line = attr.time + ' | ' + attr.name + ' (' + attr.duration + ' min)';
      if (attr.notes) line += ' — ' + attr.notes;
      lines.push(line);
    });
    lines.push('');
  });

  return lines.join('\n');
}

/**
 * Bind the results tab switcher (called once on init).
 */
function bindResultsTabs() {
  var tabs = document.querySelectorAll('.results__tab');
  tabs.forEach(function (tab) {
    tab.addEventListener('click', function () {
      var target = tab.getAttribute('data-tab');

      tabs.forEach(function (t) { t.classList.remove('results__tab--active'); });
      tab.classList.add('results__tab--active');

      document.querySelectorAll('.results__panel').forEach(function (panel) {
        panel.classList.remove('results__panel--active');
      });

      var targetPanel = document.getElementById('results-panel-' + target);
      if (targetPanel) targetPanel.classList.add('results__panel--active');

      if (target === 'map') {
        setTimeout(function () {
          renderResultsMap();
        }, 80);
      }
    });
  });
}
