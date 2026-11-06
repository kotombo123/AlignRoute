// swipe.js — Tinder-style card swipe logic
// Handles touch/mouse drag, tilt animation, and vote dispatch.

var SWIPE_THRESHOLD = 100; // px to trigger a vote
var MAX_ROTATION = 15;     // degrees at full drag

/**
 * Initialize swipe behavior on a card element.
 * @param {Element} cardEl - The card DOM element
 * @param {Object} callbacks - { onSwipeLeft, onSwipeRight, onDragMove }
 * @returns {Function} cleanup function to remove listeners
 */
function initSwipe(cardEl, callbacks) {
  var startX = 0;
  var startY = 0;
  var currentX = 0;
  var isDragging = false;

  function onStart(e) {
    isDragging = true;
    var point = e.touches ? e.touches[0] : e;
    startX = point.clientX;
    startY = point.clientY;
    currentX = 0;
    cardEl.style.transition = 'none';
    cardEl.classList.add('card--dragging');
  }

  function onMove(e) {
    if (!isDragging) return;
    var point = e.touches ? e.touches[0] : e;
    currentX = point.clientX - startX;

    var rotation = (currentX / window.innerWidth) * MAX_ROTATION;
    rotation = Math.max(-MAX_ROTATION, Math.min(MAX_ROTATION, rotation));

    cardEl.style.transform =
      'translateX(' + currentX + 'px) rotate(' + rotation + 'deg)';

    // Update overlay opacity
    var progress = Math.min(Math.abs(currentX) / SWIPE_THRESHOLD, 1);
    var yesOverlay = cardEl.querySelector('.card__overlay--yes');
    var noOverlay = cardEl.querySelector('.card__overlay--no');

    if (currentX > 0 && yesOverlay) {
      yesOverlay.style.opacity = progress * 0.6;
      if (noOverlay) noOverlay.style.opacity = 0;
    } else if (currentX < 0 && noOverlay) {
      noOverlay.style.opacity = progress * 0.6;
      if (yesOverlay) yesOverlay.style.opacity = 0;
    }

    if (callbacks.onDragMove) {
      callbacks.onDragMove(currentX, progress);
    }

    if (e.cancelable) e.preventDefault();
  }

  function onEnd() {
    if (!isDragging) return;
    isDragging = false;
    cardEl.classList.remove('card--dragging');

    if (Math.abs(currentX) >= SWIPE_THRESHOLD) {
      // Swipe accepted — animate off screen
      var direction = currentX > 0 ? 1 : -1;
      var flyX = direction * window.innerWidth * 1.5;
      var flyRotation = direction * 30;

      cardEl.style.transition = 'transform 0.4s ease-out, opacity 0.4s ease-out';
      cardEl.style.transform =
        'translateX(' + flyX + 'px) rotate(' + flyRotation + 'deg)';
      cardEl.style.opacity = '0';

      setTimeout(function () {
        if (direction > 0 && callbacks.onSwipeRight) {
          callbacks.onSwipeRight();
        } else if (direction < 0 && callbacks.onSwipeLeft) {
          callbacks.onSwipeLeft();
        }
      }, 350);
    } else {
      // Snap back
      cardEl.style.transition = 'transform 0.3s ease, opacity 0.3s ease';
      cardEl.style.transform = 'translateX(0) rotate(0)';

      var yesOverlay = cardEl.querySelector('.card__overlay--yes');
      var noOverlay = cardEl.querySelector('.card__overlay--no');
      if (yesOverlay) yesOverlay.style.opacity = 0;
      if (noOverlay) noOverlay.style.opacity = 0;
    }

    currentX = 0;
  }

  // Mouse events
  cardEl.addEventListener('mousedown', onStart);
  document.addEventListener('mousemove', onMove);
  document.addEventListener('mouseup', onEnd);

  // Touch events
  cardEl.addEventListener('touchstart', onStart, { passive: true });
  document.addEventListener('touchmove', onMove, { passive: false });
  document.addEventListener('touchend', onEnd);

  // Cleanup
  return function cleanup() {
    cardEl.removeEventListener('mousedown', onStart);
    document.removeEventListener('mousemove', onMove);
    document.removeEventListener('mouseup', onEnd);
    cardEl.removeEventListener('touchstart', onStart);
    document.removeEventListener('touchmove', onMove);
    document.removeEventListener('touchend', onEnd);
  };
}

/**
 * Programmatic swipe animation (for button clicks).
 * @param {Element} cardEl - The card DOM element
 * @param {'left'|'right'} direction
 * @param {Function} callback - Called after animation
 */
function animateSwipe(cardEl, direction, callback) {
  var dir = direction === 'right' ? 1 : -1;
  var flyX = dir * window.innerWidth * 1.5;
  var flyRotation = dir * 30;

  // Show overlay
  var overlayClass = direction === 'right' ? '.card__overlay--yes' : '.card__overlay--no';
  var overlay = cardEl.querySelector(overlayClass);
  if (overlay) overlay.style.opacity = '0.6';

  cardEl.style.transition = 'transform 0.45s ease-out, opacity 0.45s ease-out';
  cardEl.style.transform =
    'translateX(' + flyX + 'px) rotate(' + flyRotation + 'deg)';
  cardEl.style.opacity = '0';

  setTimeout(function () {
    if (callback) callback();
  }, 400);
}

// Exports
window.swipe = Object.freeze({
  initSwipe: initSwipe,
  animateSwipe: animateSwipe,
  SWIPE_THRESHOLD: SWIPE_THRESHOLD,
});
