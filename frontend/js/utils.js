// utils.js — DOM helpers and general utilities

/**
 * Select a single element by CSS selector.
 * @param {string} selector
 * @param {Element} [parent=document]
 * @returns {Element|null}
 */
function $(selector, parent) {
  return (parent || document).querySelector(selector);
}

/**
 * Select all elements matching a CSS selector.
 * @param {string} selector
 * @param {Element} [parent=document]
 * @returns {Element[]}
 */
function $$(selector, parent) {
  return Array.from((parent || document).querySelectorAll(selector));
}

/**
 * Create an element with optional attributes and children.
 * @param {string} tag - HTML tag name
 * @param {Object} [attrs] - Attribute key-value pairs (class, id, data-*, etc.)
 * @param {(string|Element)[]} [children] - Text strings or child elements
 * @returns {Element}
 */
function createElement(tag, attrs, children) {
  var el = document.createElement(tag);

  if (attrs) {
    Object.keys(attrs).forEach(function (key) {
      if (key === 'className') {
        el.className = attrs[key];
      } else if (key === 'textContent') {
        el.textContent = attrs[key];
      } else if (key === 'innerHTML') {
        el.innerHTML = attrs[key];
      } else if (key.startsWith('on') && typeof attrs[key] === 'function') {
        el.addEventListener(key.slice(2).toLowerCase(), attrs[key]);
      } else {
        el.setAttribute(key, attrs[key]);
      }
    });
  }

  if (children) {
    children.forEach(function (child) {
      if (typeof child === 'string') {
        el.appendChild(document.createTextNode(child));
      } else if (child instanceof Element) {
        el.appendChild(child);
      }
    });
  }

  return el;
}

/**
 * Show a screen by ID, hiding all others.
 * @param {string} screenId - The ID of the screen element to show
 */
function showScreen(screenId) {
  $$('.screen').forEach(function (screen) {
    screen.classList.remove('screen--active');
    screen.setAttribute('aria-hidden', 'true');
  });

  var target = document.getElementById(screenId);
  if (target) {
    target.classList.add('screen--active');
    target.setAttribute('aria-hidden', 'false');
  }
}

/**
 * Copy text to the clipboard. Falls back to execCommand for older browsers.
 * @param {string} text
 * @returns {Promise<boolean>}
 */
function copyToClipboard(text) {
  if (navigator.clipboard && navigator.clipboard.writeText) {
    return navigator.clipboard.writeText(text).then(function () {
      return true;
    }).catch(function () {
      return fallbackCopy(text);
    });
  }
  return Promise.resolve(fallbackCopy(text));
}

function fallbackCopy(text) {
  var textarea = document.createElement('textarea');
  textarea.value = text;
  textarea.style.position = 'fixed';
  textarea.style.opacity = '0';
  document.body.appendChild(textarea);
  textarea.select();
  var ok = false;
  try {
    ok = document.execCommand('copy');
  } catch (e) {
    ok = false;
  }
  document.body.removeChild(textarea);
  return ok;
}

/**
 * Generate a random 6-character join code (for demo/mock use).
 * @returns {string}
 */
function generateJoinCode() {
  var chars = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
  var code = '';
  for (var i = 0; i < 6; i++) {
    code += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return code;
}

/**
 * Debounce a function call.
 * @param {Function} fn
 * @param {number} delay - Milliseconds
 * @returns {Function}
 */
function debounce(fn, delay) {
  var timer = null;
  return function () {
    var context = this;
    var args = arguments;
    if (timer) clearTimeout(timer);
    timer = setTimeout(function () {
      fn.apply(context, args);
    }, delay);
  };
}

/**
 * Format a duration label (e.g. "3 days" / "1 day").
 * @param {number} days
 * @param {string} lang
 * @returns {string}
 */
function formatDuration(days, lang) {
  if (days === 1) {
    return '1 ' + window.i18n.t('create.duration.day', lang);
  }
  return days + ' ' + window.i18n.t('create.duration.days', lang);
}

/**
 * Simple UUID v4 generator (for local mock IDs).
 * @returns {string}
 */
function uuid() {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
    var r = (Math.random() * 16) | 0;
    var v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

// Exports
window.utils = Object.freeze({
  $: $,
  $$: $$,
  createElement: createElement,
  showScreen: showScreen,
  copyToClipboard: copyToClipboard,
  generateJoinCode: generateJoinCode,
  debounce: debounce,
  formatDuration: formatDuration,
  uuid: uuid,
});
