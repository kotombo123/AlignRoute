// app.js — Main application: routing, state, and screen orchestration

// ── State ─────────────────────────────────────────────────────────

var state = {
  currentScreen: 'landing',
  sessionId: null,
  participantId: null,
  joinCode: null,
  isOrganizer: false,
  language: 'pl',
  nickname: '',
  destination: '',
  durationDays: 3,
  participants: [],
  attractions: [],
  currentCardIndex: 0,
  votes: {},
  itinerary: null,
  excludedCategories: [],
};

// Immutable state updater — returns new state object
function updateState(patch) {
  state = Object.assign({}, state, patch);
  return state;
}

// ── Mock Data (used when backend is unreachable) ──────────────────

var MOCK_ATTRACTIONS = [
  {
    id: '1', name: 'Wawel Castle', category: 'History',
    description: 'A fortified architectural complex on the Vistula River, one of the most important historical sites in Poland.',
    lat: 50.0540, lon: 19.9352,
  },
  {
    id: '2', name: 'Main Market Square', category: 'Landmark',
    description: 'The largest medieval town square in Europe, surrounded by historic townhouses and palaces.',
    lat: 50.0616, lon: 19.9373,
  },
  {
    id: '3', name: "St. Mary's Basilica", category: 'Religion',
    description: 'A stunning brick Gothic church with the famous wooden altarpiece by Veit Stoss.',
    lat: 50.0617, lon: 19.9394,
  },
  {
    id: '4', name: 'Kazimierz District', category: 'Culture',
    description: 'The historic Jewish quarter, now a vibrant cultural hub with galleries, cafes, and street art.',
    lat: 50.0490, lon: 19.9460,
  },
  {
    id: '5', name: 'Wieliczka Salt Mine', category: 'Nature',
    description: 'An underground marvel dating to the 13th century with carved chapels and saline lakes.',
    lat: 49.9833, lon: 20.0553,
  },
  {
    id: '6', name: "Schindler's Factory", category: 'History',
    description: 'A museum documenting wartime Krakow, located in the former enamelware factory.',
    lat: 50.0475, lon: 19.9617,
  },
  {
    id: '7', name: 'Planty Park', category: 'Nature',
    description: 'A ring of green park surrounding the Old Town, perfect for a leisurely walk.',
    lat: 50.0600, lon: 19.9350,
  },
  {
    id: '8', name: 'Cloth Hall (Sukiennice)', category: 'Landmark',
    description: 'A Renaissance-era trading hall in the center of the Main Market Square.',
    lat: 50.0614, lon: 19.9375,
  },
];

var MOCK_ITINERARY = {
  days: [
    {
      dayNumber: 1,
      attractions: [
        { id: '1', name: 'Wawel Castle', time: '09:00', duration: 120, lat: 50.0540, lon: 19.9352, notes: 'Book tickets in advance' },
        { id: '2', name: 'Main Market Square', time: '12:00', duration: 60, lat: 50.0616, lon: 19.9373, notes: 'Lunch at one of the restaurants' },
        { id: '3', name: "St. Mary's Basilica", time: '14:00', duration: 45, lat: 50.0617, lon: 19.9394, notes: 'Trumpet call every hour' },
      ],
    },
    {
      dayNumber: 2,
      attractions: [
        { id: '4', name: 'Kazimierz District', time: '10:00', duration: 180, lat: 50.0490, lon: 19.9460, notes: 'Walking tour recommended' },
        { id: '6', name: "Schindler's Factory", time: '15:00', duration: 120, lat: 50.0475, lon: 19.9617, notes: 'Allow extra time for exhibits' },
      ],
    },
    {
      dayNumber: 3,
      attractions: [
        { id: '5', name: 'Wieliczka Salt Mine', time: '09:00', duration: 180, lat: 49.9833, lon: 20.0553, notes: 'Underground tour — wear warm clothes' },
        { id: '7', name: 'Planty Park', time: '15:00', duration: 60, lat: 50.0600, lon: 19.9350, notes: 'Relaxing walk around Old Town' },
      ],
    },
  ],
};

// ── Initialization ────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', function () {
  initLanguage();
  bindNavigation();
  bindCreateForm();
  bindJoinForm();
  bindVotingButtons();
  bindResultsTabs();
  checkUrlForJoinCode();
  renderLanding();
});

// ── Language ──────────────────────────────────────────────────────

function initLanguage() {
  var saved = localStorage.getItem('alignroute_lang');
  if (saved === 'en' || saved === 'pl') {
    updateState({ language: saved });
  }
  window.i18n.applyTranslations(state.language);
  updateLanguageToggle();
}

function toggleLanguage() {
  var newLang = state.language === 'pl' ? 'en' : 'pl';
  updateState({ language: newLang });
  localStorage.setItem('alignroute_lang', newLang);
  window.i18n.applyTranslations(newLang);
  updateLanguageToggle();
  // Re-render dynamic content
  if (state.currentScreen === 'lobby') renderLobbyParticipants();
  if (state.currentScreen === 'voting') updateVotingProgress();
}

function updateLanguageToggle() {
  var btn = document.getElementById('lang-toggle');
  if (btn) {
    btn.textContent = window.i18n.t('nav.language', state.language);
  }
}

// ── Navigation ────────────────────────────────────────────────────

function bindNavigation() {
  var logo = document.getElementById('nav-logo');
  if (logo) {
    logo.addEventListener('click', function () {
      navigateTo('landing');
    });
  }

  var langBtn = document.getElementById('lang-toggle');
  if (langBtn) {
    langBtn.addEventListener('click', toggleLanguage);
  }
}

function navigateTo(screen) {
  updateState({ currentScreen: screen });
  window.utils.showScreen('screen-' + screen);

  if (screen === 'landing') {
    renderLanding();
  }
}

function checkUrlForJoinCode() {
  var params = new URLSearchParams(window.location.search);
  var joinCode = params.get('join');
  if (joinCode) {
    updateState({ joinCode: joinCode });
    showJoinScreen(joinCode);
  }
}

// ── Landing Page ──────────────────────────────────────────────────

function renderLanding() {
  var ctaBtn = document.getElementById('cta-plan');
  if (ctaBtn) {
    ctaBtn.addEventListener('click', function () {
      navigateTo('create');
    });
  }

  var ctaStart = document.getElementById('cta-start');
  if (ctaStart) {
    ctaStart.addEventListener('click', function (e) {
      e.preventDefault();
      navigateTo('create');
    });
  }

  var footerCta = document.getElementById('footer-cta');
  if (footerCta) {
    footerCta.addEventListener('click', function () {
      navigateTo('create');
    });
  }

  bindHeroJoinCode();
  initDemoCard();
  initEntranceObserver();
}

function bindHeroJoinCode() {
  var input = document.getElementById('hero-join-code');
  var btn = document.getElementById('hero-join-btn');
  if (!input || !btn || btn.dataset.initialized === 'true') return;
  btn.dataset.initialized = 'true';

  function tryJoin() {
    var code = input.value.trim().toUpperCase();
    if (!code) {
      showToast(window.i18n.t('hero.join.empty', state.language));
      return;
    }
    updateState({ joinCode: code });
    showJoinScreen(code);
  }

  btn.addEventListener('click', tryJoin);
  input.addEventListener('keydown', function (e) {
    if (e.key === 'Enter') tryJoin();
  });
}

// ── Demo Swipe Card ──────────────────────────────────────────────

function initDemoCard() {
  var demoCard = document.getElementById('demo-card');
  var demoResult = document.getElementById('demo-result');
  if (!demoCard || demoCard.dataset.initialized === 'true') return;
  demoCard.dataset.initialized = 'true';

  window.swipe.initSwipe(demoCard, {
    onSwipeRight: function () {
      if (demoResult) {
        demoResult.textContent = window.i18n.t('demo.added', state.language);
        demoResult.style.display = 'flex';
      }
      setTimeout(resetDemoCard, 1800);
    },
    onSwipeLeft: function () {
      if (demoResult) {
        demoResult.textContent = window.i18n.t('demo.removed', state.language);
        demoResult.style.display = 'flex';
      }
      setTimeout(resetDemoCard, 1800);
    },
  });
}

function resetDemoCard() {
  var demoCard = document.getElementById('demo-card');
  var demoResult = document.getElementById('demo-result');
  if (demoCard) {
    demoCard.style.display = '';
    demoCard.style.transform = '';
    demoCard.style.opacity = '1';
    demoCard.style.transition = '';
    var yesOv = demoCard.querySelector('.card__overlay--yes');
    var noOv = demoCard.querySelector('.card__overlay--no');
    if (yesOv) yesOv.style.opacity = '0';
    if (noOv) noOv.style.opacity = '0';
    demoCard.dataset.initialized = '';
    initDemoCard();
  }
  if (demoResult) demoResult.style.display = 'none';
}

// ── Section Entrance Observer ────────────────────────────────────

function initEntranceObserver() {
  var targets = document.querySelectorAll('.entrance');
  if (!targets.length || typeof IntersectionObserver === 'undefined') {
    targets.forEach(function (el) { el.classList.add('entrance--visible'); });
    return;
  }

  var observer = new IntersectionObserver(function (entries) {
    entries.forEach(function (entry) {
      if (entry.isIntersecting) {
        entry.target.classList.add('entrance--visible');
        observer.unobserve(entry.target);
      }
    });
  }, { threshold: 0.15 });

  targets.forEach(function (el) { observer.observe(el); });
}

// ── Create Session ────────────────────────────────────────────────

function bindCreateForm() {
  var form = document.getElementById('create-form');
  if (!form) return;

  var slider = document.getElementById('create-duration');
  var valueDisplay = document.getElementById('duration-value');

  if (slider && valueDisplay) {
    slider.addEventListener('input', function () {
      var days = parseInt(slider.value, 10);
      updateState({ durationDays: days });
      valueDisplay.textContent = window.utils.formatDuration(days, state.language);
    });
  }

  form.addEventListener('submit', function (e) {
    e.preventDefault();
    handleCreateSession();
  });

  var backBtn = document.getElementById('create-back');
  if (backBtn) {
    backBtn.addEventListener('click', function () {
      navigateTo('landing');
    });
  }
}

function handleCreateSession() {
  var dest = document.getElementById('create-destination').value.trim();
  var nickname = document.getElementById('create-nickname').value.trim();
  var duration = parseInt(document.getElementById('create-duration').value, 10);

  if (!dest || !nickname) return;

  var submitBtn = document.getElementById('create-submit');
  if (submitBtn) {
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<span class="spinner"></span>';
  }

  window.api.createSession(dest, duration, nickname, state.language)
    .then(function (data) {
      updateState({
        sessionId: data.sessionId || data.id,
        participantId: data.participantId,
        joinCode: data.joinCode || data.code,
        isOrganizer: true,
        nickname: nickname,
        destination: dest,
        durationDays: duration,
        participants: [{ id: data.participantId, nickname: nickname, isOrganizer: true }],
      });
      connectSignalR();
      showLobby();
    })
    .catch(function (err) {
      var message = (err && err.userMessage) || window.i18n.t('error.generic', state.language);
      showToast(message);
    })
    .finally(function () {
      if (submitBtn) {
        submitBtn.disabled = false;
        submitBtn.innerHTML = '';
        submitBtn.textContent = window.i18n.t('create.submit', state.language);
      }
    });
}

// ── Join Session ──────────────────────────────────────────────────

function bindJoinForm() {
  var form = document.getElementById('join-form');
  if (!form) return;

  form.addEventListener('submit', function (e) {
    e.preventDefault();
    handleJoinSession();
  });
}

function showJoinScreen(joinCode) {
  navigateTo('join');

  var infoEl = document.getElementById('join-info');
  var formEl = document.getElementById('join-form-fields');
  var loadingEl = document.getElementById('join-loading');
  var errorEl = document.getElementById('join-error');

  if (loadingEl) loadingEl.style.display = 'block';
  if (formEl) formEl.style.display = 'none';
  if (errorEl) errorEl.style.display = 'none';

  window.api.getSession(joinCode)
    .then(function (data) {
      if (loadingEl) loadingEl.style.display = 'none';
      if (formEl) formEl.style.display = 'block';

      updateState({
        destination: data.destination,
        durationDays: data.durationDays,
        joinCode: joinCode,
      });

      var destEl = document.getElementById('join-destination');
      var durEl = document.getElementById('join-duration');
      var orgEl = document.getElementById('join-organizer');

      if (destEl) destEl.textContent = data.destination;
      if (durEl) durEl.textContent = data.durationDays + ' ' + window.i18n.t('join.days', state.language);
      if (orgEl) orgEl.textContent = data.organizerName || '';
    })
    .catch(function (err) {
      if (loadingEl) loadingEl.style.display = 'none';
      if (errorEl) {
        errorEl.textContent = (err && err.userMessage) ||
          'Could not load session. Please check the join code and try again.';
        errorEl.style.display = 'block';
      }
    });
}

function handleJoinSession() {
  var nickname = document.getElementById('join-nickname').value.trim();
  if (!nickname) return;

  window.api.joinSession(state.joinCode, nickname)
    .then(function (data) {
      var session = data.session || {};
      updateState({
        sessionId: session.id || data.sessionId,
        participantId: data.participantId,
        isOrganizer: false,
        nickname: nickname,
        participants: session.participants || data.participants || [],
      });
      connectSignalR();
      showLobby();
    })
    .catch(function (err) {
      var message = (err && err.userMessage) || window.i18n.t('error.generic', state.language);
      showToast(message);
    });
}

// ── SignalR ───────────────────────────────────────────────────────

function connectSignalR() {
  window.hub.on('ParticipantJoined', function (data) {
    var existing = state.participants.some(function (p) { return p.id === data.participantId; });
    if (!existing) {
      updateState({
        participants: state.participants.concat([{
          id: data.participantId,
          nickname: data.nickname,
          isOrganizer: false,
        }]),
      });
      renderLobbyParticipants();
    }
  });

  window.hub.on('VoteUpdate', function (data) {
    if (state.currentScreen === 'voting') {
      updateVotingSidebar();
    }
  });

  window.hub.on('ParticipantFinished', function (data) {
    if (state.currentScreen === 'voting') {
      updateVotingSidebar();
    }
  });

  window.hub.on('ExplorationComplete', function () {
    loadAttractions();
  });

  window.hub.on('PlanGenerated', function () {
    showResults();
  });

  window.hub.on('ConnectionState', function (data) {
    if (data.state === 'reconnecting') {
      showToast(state.language === 'pl'
        ? 'Utracono połączenie — próba ponownego połączenia...'
        : 'Connection lost — reconnecting...');
    } else if (data.state === 'disconnected') {
      showToast(window.i18n.t('error.network', state.language));
    }
  });

  window.hub.connect(state.sessionId);
}

// ── Lobby ─────────────────────────────────────────────────────────

function showLobby() {
  navigateTo('lobby');

  var codeEl = document.getElementById('lobby-code');
  if (codeEl) codeEl.textContent = state.joinCode;

  var shareEl = document.getElementById('lobby-share-link');
  if (shareEl) {
    var shareUrl = window.location.origin + window.location.pathname + '?join=' + state.joinCode;
    shareEl.textContent = shareUrl;
  }

  var copyBtn = document.getElementById('lobby-copy-btn');
  if (copyBtn) {
    copyBtn.addEventListener('click', function () {
      window.utils.copyToClipboard(state.joinCode).then(function () {
        showToast(window.i18n.t('lobby.copied', state.language));
      });
    });
  }

  var startBtn = document.getElementById('lobby-start-btn');
  var waitingEl = document.getElementById('lobby-waiting-msg');

  if (state.isOrganizer) {
    if (startBtn) startBtn.style.display = 'inline-flex';
    if (waitingEl) waitingEl.style.display = 'none';
    startBtn.addEventListener('click', handleStartExploration);
  } else {
    if (startBtn) startBtn.style.display = 'none';
    if (waitingEl) waitingEl.style.display = 'block';
  }

  renderLobbyParticipants();
}

function renderLobbyParticipants() {
  var listEl = document.getElementById('lobby-participants-list');
  if (!listEl) return;

  listEl.innerHTML = '';

  state.participants.forEach(function (p) {
    var initials = p.nickname.charAt(0).toUpperCase();
    var isYou = p.id === state.participantId;

    var li = document.createElement('li');
    li.className = 'lobby__participant animate-fade-in-up';

    var avatar = document.createElement('div');
    avatar.className = 'lobby__participant-avatar';
    avatar.textContent = initials;

    var nameSpan = document.createElement('span');
    nameSpan.className = 'lobby__participant-name';
    nameSpan.textContent = p.nickname;

    li.appendChild(avatar);
    li.appendChild(nameSpan);

    if (isYou) {
      var youTag = document.createElement('span');
      youTag.className = 'lobby__participant-you';
      youTag.textContent = window.i18n.t('lobby.you', state.language);
      li.appendChild(youTag);
    }

    if (p.isOrganizer) {
      var badge = document.createElement('span');
      badge.className = 'badge badge--secondary';
      badge.textContent = 'Organizer';
      badge.style.marginLeft = 'auto';
      li.appendChild(badge);
    }

    listEl.appendChild(li);
  });
}

function handleStartExploration() {
  var startBtn = document.getElementById('lobby-start-btn');
  if (startBtn) {
    startBtn.disabled = true;
    startBtn.innerHTML = '<span class="spinner"></span>';
  }

  window.api.startExploration(state.sessionId)
    .then(function () {
      loadAttractions();
    })
    .catch(function (err) {
      var msg = (err && err.userMessage) || window.i18n.t('error.network', state.language);
      showToast(msg);
      // Mock mode fallback when backend unreachable
      updateState({ attractions: MOCK_ATTRACTIONS });
      showVoting();
    })
    .finally(function () {
      if (startBtn) {
        startBtn.disabled = false;
        startBtn.textContent = window.i18n.t('lobby.start', state.language);
      }
    });
}

function loadAttractions() {
  window.api.getAttractions(state.sessionId)
    .then(function (data) {
      var raw = data.attractions || data || [];
      var attractions = raw.map(function (a) {
        return Object.assign({}, a, {
          lat: a.lat !== undefined ? a.lat : a.latitude,
          lon: a.lon !== undefined ? a.lon : a.longitude,
        });
      });
      updateState({ attractions: attractions });
      showVoting();
    })
    .catch(function (err) {
      var msg = (err && err.userMessage) || window.i18n.t('error.network', state.language);
      showToast(msg);
      updateState({ attractions: MOCK_ATTRACTIONS });
      showVoting();
    });
}

// Voting and Results logic are in voting.js and results.js respectively.
// They are loaded before app.js and share the global state/functions above.

// ── Toast ─────────────────────────────────────────────────────────

function showToast(message) {
  var toast = document.getElementById('toast');
  if (!toast) return;

  toast.textContent = message;
  toast.classList.add('toast--visible');

  setTimeout(function () {
    toast.classList.remove('toast--visible');
  }, 2500);
}
