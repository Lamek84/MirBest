// Warenkorb-Checkout: zeigt/versteckt die Lieferadresse-Felder je nach gewählter
// Versandart (bei Selbstabholung nicht nötig). Ausgelagert aus Cart/Index.cshtml,
// weil unsere CSP (script-src 'self') keine Inline-<script>-Blöcke erlaubt.
document.addEventListener('DOMContentLoaded', function () {
    var radios = document.querySelectorAll('.delivery-radio');
    var addressBox = document.getElementById('shippingAddressFields');
    var requiredFields = addressBox
        ? addressBox.querySelectorAll('#shippingName, #shippingStreet, #shippingPostalCode, #shippingCity')
        : [];

    function updateAddressVisibility() {
        var selected = document.querySelector('.delivery-radio:checked');
        var isPickup = selected && selected.getAttribute('data-pickup') === 'true';

        if (!addressBox) {
            return;
        }

        addressBox.style.display = isPickup ? 'none' : 'block';
        requiredFields.forEach(function (field) {
            if (isPickup) {
                field.removeAttribute('required');
            } else {
                field.setAttribute('required', 'required');
            }
        });
    }

    radios.forEach(function (radio) {
        radio.addEventListener('change', updateAddressVisibility);
    });

    updateAddressVisibility();
});
