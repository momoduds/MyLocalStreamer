(function () {
    var theme = 'dark';
    try {
        if (localStorage.getItem('themePreference') === 'light') theme = 'light';
    } catch (e) { /* storage blocked - keep default */ }
    document.documentElement.dataset.theme = theme;
})();
