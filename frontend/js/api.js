// api.js — Backend API communication layer
// All calls target the gateway at API_BASE.
// When served through Caddy (localhost:8080 or prod domain), /api/* is proxied
// to the .NET backend automatically — no hardcoded port needed.

var API_BASE = window.ALIGN_API_BASE || '/api';

/**
 * Generic fetch wrapper with error handling.
 * @param {string} path - URL path relative to API_BASE
 * @param {Object} [options] - fetch options override
 * @returns {Promise<Object>} parsed JSON body
 */
function apiFetch(path, options) {
  var url = API_BASE + path;
  var defaults = {
    headers: { 'Content-Type': 'application/json' },
  };
  var merged = Object.assign({}, defaults, options || {});

  return fetch(url, merged)
    .then(function (res) {
      if (!res.ok) {
        return res.text().then(function (body) {
          var message = 'Something went wrong (error ' + res.status + ')';
          try {
            var parsed = JSON.parse(body);
            if (parsed.error) {
              message = parsed.error;
            } else if (parsed.detail) {
              message = parsed.detail;
            }
          } catch (_ignore) {
            // body is not JSON — use the default message
          }
          var err = new Error(message);
          err.status = res.status;
          err.body = body;
          err.userMessage = message;
          throw err;
        });
      }
      var contentType = res.headers.get('content-type') || '';
      if (contentType.indexOf('application/json') !== -1) {
        return res.json();
      }
      return res.text();
    })
    .catch(function (err) {
      if (err.status) {
        // Already an API error with status — re-throw as-is
        throw err;
      }
      // Network error (no response received)
      var networkErr = new Error('Network error: could not reach the server.');
      networkErr.status = 0;
      networkErr.userMessage = 'Could not reach the server. Please check your connection.';
      throw networkErr;
    });
}

// ── Sessions ──────────────────────────────────────────────────────

function createSession(destination, durationDays, nickname, language) {
  return apiFetch('/sessions', {
    method: 'POST',
    body: JSON.stringify({
      destination: destination,
      durationDays: durationDays,
      organizerNickname: nickname,
      language: language,
    }),
  });
}

function getSession(joinCode) {
  return apiFetch('/sessions/' + encodeURIComponent(joinCode));
}

function joinSession(joinCode, nickname) {
  return apiFetch('/sessions/' + encodeURIComponent(joinCode) + '/join', {
    method: 'POST',
    body: JSON.stringify({ nickname: nickname }),
  });
}

function getSessionStatus(sessionId) {
  return apiFetch('/sessions/' + encodeURIComponent(sessionId) + '/status');
}

// ── Exploration ───────────────────────────────────────────────────

function startExploration(sessionId) {
  return apiFetch('/sessions/' + encodeURIComponent(sessionId) + '/explore', {
    method: 'POST',
  });
}

function getAttractions(sessionId) {
  return apiFetch('/sessions/' + encodeURIComponent(sessionId) + '/attractions');
}

// ── Voting ────────────────────────────────────────────────────────

function getCards(sessionId, participantId) {
  return apiFetch(
    '/sessions/' + encodeURIComponent(sessionId) +
    '/cards/' + encodeURIComponent(participantId)
  );
}

function castVote(sessionId, participantId, attractionId, isPositive) {
  return apiFetch('/sessions/' + encodeURIComponent(sessionId) + '/vote', {
    method: 'POST',
    body: JSON.stringify({
      participantId: participantId,
      attractionId: attractionId,
      isPositive: isPositive,
    }),
  });
}

function updateExclusions(sessionId, participantId, categories) {
  return apiFetch(
    '/sessions/' + encodeURIComponent(sessionId) +
    '/participants/' + encodeURIComponent(participantId) +
    '/exclusions',
    {
      method: 'PUT',
      body: JSON.stringify({ categories: categories }),
    }
  );
}

function markDone(sessionId, participantId) {
  return apiFetch(
    '/sessions/' + encodeURIComponent(sessionId) +
    '/participants/' + encodeURIComponent(participantId) +
    '/done',
    {
      method: 'PUT',
    }
  );
}

// ── Itinerary ─────────────────────────────────────────────────────

function generatePlan(sessionId) {
  return apiFetch('/sessions/' + encodeURIComponent(sessionId) + '/generate-plan', {
    method: 'POST',
  });
}

function getItinerary(sessionId) {
  return apiFetch('/sessions/' + encodeURIComponent(sessionId) + '/itinerary');
}

// Exports
window.api = Object.freeze({
  createSession: createSession,
  getSession: getSession,
  joinSession: joinSession,
  getSessionStatus: getSessionStatus,
  startExploration: startExploration,
  getAttractions: getAttractions,
  getCards: getCards,
  castVote: castVote,
  updateExclusions: updateExclusions,
  markDone: markDone,
  generatePlan: generatePlan,
  getItinerary: getItinerary,
});
