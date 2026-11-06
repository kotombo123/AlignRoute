// voting.js — Voting screen logic: card rendering, swipe, exclusions

var currentSwipeCleanup = null;

/**
 * Show the voting screen and initialize cards.
 */
function showVoting() {
  updateState({ currentCardIndex: 0, votes: {}, excludedCategories: [] });
  navigateTo('voting');
  renderExclusionChips();
  updateVotingProgress();
  renderCurrentCard();
  updateVotingSidebar();
}

/**
 * Bind event listeners for voting buttons (called once on init).
 */
function bindVotingButtons() {
  var noBtn = document.getElementById('vote-no');
  var yesBtn = document.getElementById('vote-yes');
  var doneBtn = document.getElementById('voting-done-btn');
  var endBtn = document.getElementById('voting-end-btn');

  if (noBtn) {
    noBtn.addEventListener('click', function () {
      handleVote(false);
    });
  }

  if (yesBtn) {
    yesBtn.addEventListener('click', function () {
      handleVote(true);
    });
  }

  if (doneBtn) {
    doneBtn.addEventListener('click', function () {
      window.api.markDone(state.sessionId, state.participantId).catch(function () {});
      doneBtn.disabled = true;
      doneBtn.textContent = window.i18n.t('voting.waiting', state.language);
    });
  }

  if (endBtn) {
    endBtn.addEventListener('click', function () {
      endBtn.disabled = true;
      endBtn.innerHTML = '<span class="spinner"></span>';
      window.api.generatePlan(state.sessionId)
        .then(function () { showResults(); })
        .catch(function () { showResults(); });
    });
  }
}

/**
 * Get attractions after applying category exclusions.
 * @returns {Array}
 */
function getFilteredAttractions() {
  if (state.excludedCategories.length === 0) return state.attractions;
  return state.attractions.filter(function (a) {
    return state.excludedCategories.indexOf(a.category) === -1;
  });
}

/**
 * Update the progress bar and counter text.
 */
function updateVotingProgress() {
  var filtered = getFilteredAttractions();
  var current = Math.min(state.currentCardIndex + 1, filtered.length);
  var total = filtered.length;

  var progressText = document.getElementById('voting-progress-text');
  if (progressText) {
    progressText.textContent = window.i18n.t('voting.progress', state.language, {
      current: current.toString(),
      total: total.toString(),
    });
  }

  var progressFill = document.getElementById('voting-progress-fill');
  if (progressFill) {
    var pct = total > 0 ? (current / total) * 100 : 0;
    progressFill.style.width = pct + '%';
  }

  var endBtn = document.getElementById('voting-end-btn');
  if (endBtn) {
    endBtn.style.display = state.isOrganizer ? 'inline-flex' : 'none';
  }

  var doneBtn = document.getElementById('voting-done-btn');
  if (doneBtn) {
    doneBtn.style.display = (state.currentCardIndex >= filtered.length) ? 'inline-flex' : 'none';
  }
}

/**
 * Render the current attraction card (and a preview of the next one behind it).
 */
function renderCurrentCard() {
  var stack = document.getElementById('card-stack');
  if (!stack) return;

  if (currentSwipeCleanup) {
    currentSwipeCleanup();
    currentSwipeCleanup = null;
  }

  stack.innerHTML = '';

  var filtered = getFilteredAttractions();

  if (state.currentCardIndex >= filtered.length) {
    var emptyDiv = document.createElement('div');
    emptyDiv.className = 'card-stack__empty';
    emptyDiv.textContent = window.i18n.t('voting.no.cards', state.language);
    stack.appendChild(emptyDiv);
    updateVotingProgress();
    return;
  }

  // Next card behind current
  if (state.currentCardIndex + 1 < filtered.length) {
    var nextCard = createAttractionCard(filtered[state.currentCardIndex + 1], true);
    stack.appendChild(nextCard);
  }

  // Current card on top
  var current = filtered[state.currentCardIndex];
  var cardEl = createAttractionCard(current, false);
  stack.appendChild(cardEl);

  currentSwipeCleanup = window.swipe.initSwipe(cardEl, {
    onSwipeRight: function () { handleVoteFromSwipe(true); },
    onSwipeLeft: function () { handleVoteFromSwipe(false); },
  });
}

/**
 * Create a single attraction card DOM element.
 * @param {Object} attraction
 * @param {boolean} isNext - true for the background/stacked card
 * @returns {Element}
 */
function appendCardMap(container, attraction) {
  var lat = attraction.lat;
  var lon = attraction.lon;

  if (attraction.imageUrl) {
    var img = document.createElement('img');
    img.style.cssText = 'width:100%;height:100%;object-fit:cover;display:block;';
    img.src = attraction.imageUrl;
    img.alt = attraction.name;
    img.onerror = function () {
      container.innerHTML = '';
      appendCardMap(container, Object.assign({}, attraction, { imageUrl: null }));
    };
    container.appendChild(img);
    return;
  }

  if (!lat || !lon) {
    container.style.background = 'var(--surface)';
    return;
  }

  var bbox = [
    (lon - 0.006).toFixed(6),
    (lat - 0.004).toFixed(6),
    (lon + 0.006).toFixed(6),
    (lat + 0.004).toFixed(6),
  ].join('%2C');
  var markerParam = lat.toFixed(6) + '%2C' + lon.toFixed(6);

  var frame = document.createElement('iframe');
  frame.src = 'https://www.openstreetmap.org/export/embed.html'
    + '?bbox=' + bbox
    + '&layer=mapnik'
    + '&marker=' + markerParam;
  frame.style.cssText = 'width:100%;height:100%;border:none;';
  frame.setAttribute('loading', 'lazy');
  frame.setAttribute('title', attraction.name);

  // Transparent overlay so touch events reach the card for swiping
  var overlay = document.createElement('div');
  overlay.style.cssText = 'position:absolute;inset:0;z-index:1;';

  container.appendChild(frame);
  container.appendChild(overlay);
}

function createAttractionCard(attraction, isNext) {
  var card = document.createElement('div');
  card.className = 'attraction-card' + (isNext ? ' attraction-card--next' : ' attraction-card--current');

  var mapDiv = document.createElement('div');
  mapDiv.className = 'attraction-card__map';

  appendCardMap(mapDiv, attraction);

  card.appendChild(mapDiv);

  var body = document.createElement('div');
  body.className = 'attraction-card__body';

  var category = document.createElement('span');
  category.className = 'attraction-card__category';
  category.textContent = attraction.category;

  var name = document.createElement('h3');
  name.className = 'attraction-card__name';
  name.textContent = attraction.name;

  var desc = document.createElement('p');
  desc.className = 'attraction-card__description';
  desc.textContent = attraction.description;

  body.appendChild(category);
  body.appendChild(name);
  body.appendChild(desc);
  card.appendChild(body);

  var yesOverlay = document.createElement('div');
  yesOverlay.className = 'card__overlay card__overlay--yes';
  yesOverlay.innerHTML = '<span class="card__overlay-label">YES</span>';

  var noOverlay = document.createElement('div');
  noOverlay.className = 'card__overlay card__overlay--no';
  noOverlay.innerHTML = '<span class="card__overlay-label">NO</span>';

  card.appendChild(yesOverlay);
  card.appendChild(noOverlay);

  return card;
}

/**
 * Handle a vote from the yes/no buttons.
 * @param {boolean} isPositive
 */
function handleVote(isPositive) {
  var filtered = getFilteredAttractions();
  if (state.currentCardIndex >= filtered.length) return;

  var cardEl = document.querySelector('.attraction-card--current');
  if (!cardEl) return;

  var direction = isPositive ? 'right' : 'left';
  window.swipe.animateSwipe(cardEl, direction, function () {
    recordVote(isPositive);
  });
}

function handleVoteFromSwipe(isPositive) {
  recordVote(isPositive);
}

/**
 * Record a vote locally and send to the API.
 * @param {boolean} isPositive
 */
function recordVote(isPositive) {
  var filtered = getFilteredAttractions();
  var attraction = filtered[state.currentCardIndex];
  if (!attraction) return;

  var newVotes = Object.assign({}, state.votes);
  newVotes[attraction.id] = isPositive;
  updateState({ votes: newVotes, currentCardIndex: state.currentCardIndex + 1 });

  window.api.castVote(state.sessionId, state.participantId, attraction.id, isPositive)
    .catch(function () {});

  renderCurrentCard();
  updateVotingProgress();
}

/**
 * Update the sidebar showing participant vote progress.
 */
function updateVotingSidebar() {
  var container = document.getElementById('voting-participants');
  if (!container) return;

  container.innerHTML = '';
  state.participants.forEach(function (p) {
    var row = document.createElement('div');
    row.className = 'voting__participant-row';

    var nameSpan = document.createElement('span');
    nameSpan.textContent = p.nickname;

    var countSpan = document.createElement('span');
    countSpan.className = 'voting__participant-count';
    if (p.id === state.participantId) {
      var votedCount = Object.keys(state.votes).length;
      countSpan.textContent = votedCount + '/' + getFilteredAttractions().length;
    } else {
      countSpan.textContent = '...';
    }

    row.appendChild(nameSpan);
    row.appendChild(countSpan);
    container.appendChild(row);
  });
}

// ── Category Exclusions ───────────────────────────────────────────

/**
 * Render category exclusion chips.
 */
function renderExclusionChips() {
  var container = document.getElementById('exclusion-chips');
  if (!container) return;

  var categories = [];
  state.attractions.forEach(function (a) {
    if (categories.indexOf(a.category) === -1) {
      categories.push(a.category);
    }
  });

  container.innerHTML = '';
  categories.forEach(function (cat) {
    var chip = document.createElement('button');
    chip.className = 'chip';
    chip.type = 'button';
    if (state.excludedCategories.indexOf(cat) !== -1) {
      chip.classList.add('chip--active');
    }
    chip.textContent = cat;
    chip.addEventListener('click', function () {
      toggleExclusion(cat);
    });
    container.appendChild(chip);
  });
}

/**
 * Toggle a category exclusion on/off.
 * @param {string} category
 */
function toggleExclusion(category) {
  var idx = state.excludedCategories.indexOf(category);
  var newExcluded;
  if (idx === -1) {
    newExcluded = state.excludedCategories.concat([category]);
  } else {
    newExcluded = state.excludedCategories.filter(function (c) { return c !== category; });
  }
  updateState({ excludedCategories: newExcluded });
  renderExclusionChips();
  renderCurrentCard();
  updateVotingProgress();

  window.api.updateExclusions(state.sessionId, state.participantId, newExcluded)
    .catch(function () {});
}
