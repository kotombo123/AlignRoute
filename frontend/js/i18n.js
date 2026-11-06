// i18n.js — Bilingual translation system (PL/EN)
// Uses data-i18n attributes on HTML elements for automatic text swapping.

var translations = Object.freeze({
  pl: Object.freeze({
    // Hero
    'hero.headline': 'Jedna trasa dla całej grupy',
    'hero.sentence': 'Wybierz destynację, przesuń karty, a AI ułoży plan dnia po dniu.',
    'hero.cta': 'Zaplanuj wyjazd',
    'hero.link.how': 'Jak to działa',
    'hero.link.features': 'Funkcje',
    'hero.link.team': 'Zespół',
    'hero.join.placeholder': 'Wpisz kod grupy',
    'hero.join.btn': 'Dołącz',
    'hero.join.empty': 'Wpisz kod grupy.',

    // Demo card
    'demo.desc': 'Legendarna rzeźba smoka ziejącego ogniem u stóp Wawelu.',
    'demo.hint': 'przesuń kartę w lewo lub prawo',
    'demo.added': 'Dodano do trasy!',
    'demo.removed': 'Pominięto',

    // How it works
    'howit.heading': 'Twoja podróż, wydrukowana',
    'howit.paragraph': 'Od pomysłu do gotowego planu w kilka minut. Wybierz kierunek, a reszta dzieje się sama.',
    'howit.arrival': 'Przyjazd',
    'howit.choose': 'Wybierz cel podróży',
    'howit.explore': 'AI przeszukuje okolicę',
    'howit.midday': 'Południe',
    'howit.swipe': 'Grupa przesuwa karty',
    'howit.tally': 'Głosy zliczone',
    'howit.departure': 'Wyjazd',
    'howit.build': 'AI buduje trasę',
    'howit.instant': 'natychmiast',
    'howit.final': 'Jedna trasa, uzgodniona',

    // Features
    'features.heading': 'Co robi AlignRoute',
    'features.ai.name': 'AI Explorer',
    'features.ai.desc': 'AI odnajduje to, co warte zobaczenia',
    'features.voting.name': 'Głosowanie grupy',
    'features.voting.desc': 'Jedno przesunięcie na znajomego',
    'features.route.name': 'Inteligentna trasa',
    'features.route.desc': 'Zoptymalizowane dni pod względem odległości',

    // Team letter
    'letter.heading': 'Od zespołu',
    'letter.p1': 'AlignRoute powstał po wycieczce do Barcelony, która prawie się nie udała. Pięć osób, pięć różnych pomysłów na każdy dzień. Połowa grupy chciała na plażę, reszta do muzeum.',
    'letter.p2': 'Spędziliśmy więcej czasu na kłóceniu się niż na zwiedzaniu. Po powrocie pomyśleliśmy — musi być lepszy sposób.',
    'letter.p3': 'Zaczęliśmy od prostego pytania: co jeśli każdy może zagłosować na atrakcje, które go interesują, a algorytm ułoży z tego sensowną trasę?',
    'letter.p4': 'Tak powstał AlignRoute. Aplikacja, która zamienia chaos grupowego planowania w jeden, uzgodniony plan podróży.',
    'letter.p5': 'Mamy nadzieję, że Wasze wyjazdy będą łatwiejsze niż nasz pierwszy.',
    'letter.sign': '— Zespół AlignRoute',

    // CTA
    'cta.text': 'Zaplanuj następną podróż razem',
    'cta.link': 'Zacznij teraz',

    // Footer
    'footer.statement': 'AlignRoute łączy podróżników w jedną trasę',
    'footer.project': 'Projekt akademicki · 2026',
    'footer.trip': 'Zaplanuj wyjazd',

    // SPA screens — Create
    'create.title': 'Nowa sesja',
    'create.destination': 'Cel podróży',
    'create.destination.placeholder': 'np. Kraków, Barcelona...',
    'create.duration': 'Czas trwania',
    'create.duration.days': 'dni',
    'create.duration.day': 'dzień',
    'create.nickname': 'Twój pseudonim',
    'create.nickname.placeholder': 'np. Jan',
    'create.language': 'Język',
    'create.submit': 'Utwórz sesję',
    'create.back': 'Powrót',

    // SPA screens — Join
    'join.title': 'Dołącz do sesji',
    'join.destination': 'Cel podróży',
    'join.duration': 'Czas trwania',
    'join.organizer': 'Organizator',
    'join.nickname': 'Twój pseudonim',
    'join.nickname.placeholder': 'np. Jan',
    'join.submit': 'Dołącz',
    'join.loading': 'Ładowanie sesji...',
    'join.error': 'Nie znaleziono sesji.',
    'join.days': 'dni',

    // SPA screens — Lobby
    'lobby.title': 'Poczekalnia',
    'lobby.code': 'Kod sesji',
    'lobby.copy': 'Kopiuj',
    'lobby.copied': 'Skopiowano!',
    'lobby.share': 'Udostępnij link',
    'lobby.participants': 'Uczestnicy',
    'lobby.start': 'Rozpocznij eksplorację',
    'lobby.waiting': 'Czekamy na organizatora...',
    'lobby.you': '(Ty)',

    // SPA screens — Voting
    'voting.title': 'Głosowanie',
    'voting.progress': '{current}/{total} kart',
    'voting.exclude': 'Wyklucz kategorie',
    'voting.participants': 'Uczestnicy',
    'voting.done': 'Zakończyłem głosowanie',
    'voting.end': 'Zakończ głosowanie',
    'voting.cards.of': 'z',
    'voting.no.cards': 'Brak kart do głosowania',
    'voting.waiting': 'Czekamy na wyniki...',
    'voting.voted': 'zagłosował',

    // SPA screens — Results
    'results.title': 'Wyniki i plan podróży',
    'results.summary': 'Podsumowanie głosowania',
    'results.yes': 'Tak',
    'results.no': 'Nie',
    'results.itinerary': 'Plan dnia po dniu',
    'results.day': 'Dzień',
    'results.map': 'Mapa',
    'results.share': 'Udostępnij plan',
    'results.copy': 'Kopiuj plan jako tekst',
    'results.copied': 'Skopiowano!',
    'results.generating': 'Generowanie planu...',
    'results.duration': 'Czas',
    'results.minutes': 'min',

    // Navigation
    'nav.language': 'EN',
    'nav.home': 'Strona główna',
    'nav.new': 'Nowa sesja',

    // Errors
    'error.generic': 'Wystąpił błąd. Spróbuj ponownie.',
    'error.network': 'Błąd połączenia z serwerem.',
    'error.session.not.found': 'Sesja nie została znaleziona.',
  }),

  en: Object.freeze({
    // Hero
    'hero.headline': 'One route for the whole group',
    'hero.sentence': 'Pick a destination, swipe the cards, and AI builds your day-by-day plan.',
    'hero.cta': 'Plan a trip',
    'hero.link.how': 'How it works',
    'hero.link.features': 'Features',
    'hero.link.team': 'Team',
    'hero.join.placeholder': 'Enter team code',
    'hero.join.btn': 'Join',
    'hero.join.empty': 'Please enter a team code.',

    // Demo card
    'demo.desc': 'Legendary fire-breathing dragon sculpture at the foot of Wawel Hill.',
    'demo.hint': 'swipe the card left or right',
    'demo.added': 'Added to your route!',
    'demo.removed': 'Skipped',

    // How it works
    'howit.heading': 'Your trip, printed',
    'howit.paragraph': 'From idea to finished plan in minutes. Pick a direction, and the rest happens on its own.',
    'howit.arrival': 'Arrival',
    'howit.choose': 'Choose your destination',
    'howit.explore': 'AI explores the area',
    'howit.midday': 'Midday',
    'howit.swipe': 'Group swipes on cards',
    'howit.tally': 'Votes tallied',
    'howit.departure': 'Departure',
    'howit.build': 'AI builds the route',
    'howit.instant': 'instant',
    'howit.final': 'One route, aligned',

    // Features
    'features.heading': 'What AlignRoute does',
    'features.ai.name': 'AI Explorer',
    'features.ai.desc': 'AI finds what\'s worth seeing',
    'features.voting.name': 'Group Voting',
    'features.voting.desc': 'One swipe per friend',
    'features.route.name': 'Smart Route',
    'features.route.desc': 'Distance-optimized days',

    // Team letter
    'letter.p1': 'AlignRoute started after a trip to Barcelona that almost didn\'t happen. Five people, five different ideas for every day. Half the group wanted the beach, the rest wanted museums.',
    'letter.p2': 'We spent more time arguing than sightseeing. When we got back, we thought — there has to be a better way.',
    'letter.p3': 'We started with a simple question: what if everyone could vote on the attractions they care about, and an algorithm would build a sensible route from those votes?',
    'letter.p4': 'That\'s how AlignRoute was born. An app that turns the chaos of group planning into one aligned travel plan.',
    'letter.p5': 'We hope your trips go smoother than our first one.',
    'letter.sign': '— The AlignRoute Team',

    // CTA
    'cta.text': 'Plan your next trip together',
    'cta.link': 'Start now',

    // Footer
    'footer.statement': 'AlignRoute connects travelers into one route',
    'footer.project': 'Academic project · 2026',
    'footer.trip': 'Plan a trip',

    // SPA screens — Create
    'create.title': 'New Session',
    'create.destination': 'Destination',
    'create.destination.placeholder': 'e.g. Krakow, Barcelona...',
    'create.duration': 'Trip duration',
    'create.duration.days': 'days',
    'create.duration.day': 'day',
    'create.nickname': 'Your nickname',
    'create.nickname.placeholder': 'e.g. John',
    'create.language': 'Language',
    'create.submit': 'Create session',
    'create.back': 'Back',

    // SPA screens — Join
    'join.title': 'Join Session',
    'join.destination': 'Destination',
    'join.duration': 'Duration',
    'join.organizer': 'Organizer',
    'join.nickname': 'Your nickname',
    'join.nickname.placeholder': 'e.g. John',
    'join.submit': 'Join',
    'join.loading': 'Loading session...',
    'join.error': 'Session not found.',
    'join.days': 'days',

    // SPA screens — Lobby
    'lobby.title': 'Waiting Room',
    'lobby.code': 'Session code',
    'lobby.copy': 'Copy',
    'lobby.copied': 'Copied!',
    'lobby.share': 'Share link',
    'lobby.participants': 'Participants',
    'lobby.start': 'Start exploration',
    'lobby.waiting': 'Waiting for organizer...',
    'lobby.you': '(You)',

    // SPA screens — Voting
    'voting.title': 'Voting',
    'voting.progress': '{current}/{total} cards',
    'voting.exclude': 'Exclude categories',
    'voting.participants': 'Participants',
    'voting.done': 'Done voting',
    'voting.end': 'End voting',
    'voting.cards.of': 'of',
    'voting.no.cards': 'No cards to vote on',
    'voting.waiting': 'Waiting for results...',
    'voting.voted': 'voted',

    // SPA screens — Results
    'results.title': 'Results & Itinerary',
    'results.summary': 'Voting Summary',
    'results.yes': 'Yes',
    'results.no': 'No',
    'results.itinerary': 'Day-by-Day Plan',
    'results.day': 'Day',
    'results.map': 'Map',
    'results.share': 'Share plan',
    'results.copy': 'Copy plan as text',
    'results.copied': 'Copied!',
    'results.generating': 'Generating plan...',
    'results.duration': 'Duration',
    'results.minutes': 'min',

    // Navigation
    'nav.language': 'PL',
    'nav.home': 'Home',
    'nav.new': 'New session',

    // Errors
    'error.generic': 'An error occurred. Please try again.',
    'error.network': 'Could not connect to server.',
    'error.session.not.found': 'Session not found.',
  }),
});

/**
 * Get a translated string by key, with optional parameter replacement.
 * @param {string} key - The translation key
 * @param {string} lang - Language code ('pl' or 'en')
 * @param {Object} [params] - Key-value pairs for placeholder replacement
 * @returns {string} The translated string
 */
function t(key, lang, params) {
  var langDict = translations[lang] || translations.pl;
  var text = langDict[key] || translations.pl[key] || key;

  if (params) {
    Object.keys(params).forEach(function (paramKey) {
      text = text.replace(new RegExp('\\{' + paramKey + '\\}', 'g'), params[paramKey]);
    });
  }

  return text;
}

/**
 * Apply translations to all elements with data-i18n attributes.
 * @param {string} lang - Language code ('pl' or 'en')
 */
function applyTranslations(lang) {
  var elements = document.querySelectorAll('[data-i18n]');
  elements.forEach(function (el) {
    var key = el.getAttribute('data-i18n');
    el.textContent = t(key, lang);
  });

  var placeholders = document.querySelectorAll('[data-i18n-placeholder]');
  placeholders.forEach(function (el) {
    var key = el.getAttribute('data-i18n-placeholder');
    el.placeholder = t(key, lang);
  });

  var ariaLabels = document.querySelectorAll('[data-i18n-aria]');
  ariaLabels.forEach(function (el) {
    var key = el.getAttribute('data-i18n-aria');
    el.setAttribute('aria-label', t(key, lang));
  });

  document.documentElement.lang = lang;
}

// Exports for use in app.js
window.i18n = Object.freeze({ t: t, applyTranslations: applyTranslations, translations: translations });
