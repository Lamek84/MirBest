// Gemeinsames JS für alle Seiten: Burger-Menü (mobile Navigation) und Cookie-Banner.
// Ausgelagert aus _Layout.cshtml, weil unsere Content-Security-Policy (script-src 'self')
// keine Inline-<script>-Blöcke erlaubt — nur Dateien vom eigenen Origin.
document.addEventListener('DOMContentLoaded', function () {
    var burgerBtn = document.getElementById('burgerBtn');
    var navRight = document.getElementById('navRight');
    if (burgerBtn && navRight) {
        burgerBtn.addEventListener('click', function () {
            var isOpen = navRight.classList.toggle('open');
            burgerBtn.classList.toggle('open', isOpen);
            burgerBtn.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
        });
    }

    var cookieBannerAccept = document.getElementById('cookie-banner-accept');
    if (cookieBannerAccept) {
        cookieBannerAccept.addEventListener('click', function () {
            document.cookie = 'CookieConsent=1; path=/; max-age=' + (60 * 60 * 24 * 365);
            var banner = document.getElementById('cookie-banner');
            if (banner) {
                banner.style.display = 'none';
            }
        });
    }
});
