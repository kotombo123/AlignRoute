// signalr-client.js — SignalR hub connection management
// Connects to the session hub for real-time updates.

var HUB_URL = (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1')
  ? 'http://localhost:5038/hubs/session'
  : (window.ALIGN_HUB_URL || 'http://localhost:5038/hubs/session');

var connection = null;
var eventHandlers = {};

/**
 * Register an event handler for a SignalR event.
 * @param {string} event - Event name (e.g. 'ParticipantJoined')
 * @param {Function} handler - Callback function
 */
function on(event, handler) {
  if (!eventHandlers[event]) {
    eventHandlers[event] = [];
  }
  eventHandlers[event] = eventHandlers[event].concat([handler]);
}

/**
 * Remove all handlers for an event, or a specific handler.
 * @param {string} event
 * @param {Function} [handler]
 */
function off(event, handler) {
  if (!eventHandlers[event]) return;

  if (handler) {
    eventHandlers[event] = eventHandlers[event].filter(function (h) {
      return h !== handler;
    });
  } else {
    eventHandlers[event] = [];
  }
}

/**
 * Dispatch an event to registered handlers.
 * @param {string} event
 * @param {*} data
 */
function dispatch(event, data) {
  var handlers = eventHandlers[event] || [];
  handlers.forEach(function (h) {
    try {
      h(data);
    } catch (err) {
      console.error('[SignalR] Handler error for ' + event + ':', err);
    }
  });
}

/**
 * Connect to the SignalR hub.
 * @param {string} sessionId - Session to join
 * @returns {Promise<void>}
 */
function connect(sessionId) {
  if (typeof signalR === 'undefined') {
    console.warn('[SignalR] Library not loaded — real-time features disabled.');
    return Promise.resolve();
  }

  connection = new signalR.HubConnectionBuilder()
    .withUrl(HUB_URL)
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  // Register all known events
  var events = [
    'ParticipantJoined',
    'VoteUpdate',
    'ParticipantFinished',
    'ExplorationComplete',
    'PlanGenerated',
  ];

  events.forEach(function (evt) {
    connection.on(evt, function (data) {
      dispatch(evt, data);
    });
  });

  connection.onreconnecting(function () {
    dispatch('ConnectionState', { state: 'reconnecting' });
  });

  connection.onreconnected(function () {
    dispatch('ConnectionState', { state: 'connected' });
    if (sessionId) {
      connection.invoke('JoinSessionGroup', sessionId).catch(function (err) {
        console.error('[SignalR] Rejoin failed:', err);
      });
    }
  });

  connection.onclose(function () {
    dispatch('ConnectionState', { state: 'disconnected' });
  });

  return connection.start()
    .then(function () {
      dispatch('ConnectionState', { state: 'connected' });
      if (sessionId) {
        return connection.invoke('JoinSessionGroup', sessionId);
      }
    })
    .catch(function (err) {
      console.warn('[SignalR] Connection failed:', err.message);
    });
}

/**
 * Disconnect from the hub.
 * @returns {Promise<void>}
 */
function disconnect() {
  if (connection) {
    return connection.stop().then(function () {
      connection = null;
    });
  }
  return Promise.resolve();
}

/**
 * Check if connected.
 * @returns {boolean}
 */
function isConnected() {
  return connection !== null &&
    connection.state === signalR.HubConnectionState.Connected;
}

// Exports
window.hub = Object.freeze({
  connect: connect,
  disconnect: disconnect,
  isConnected: isConnected,
  on: on,
  off: off,
});
