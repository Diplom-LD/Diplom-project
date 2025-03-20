document.addEventListener('DOMContentLoaded', () => {
    const openPopupBtn = document.getElementById('openPopupBtn');
    const popupIcon = openPopupBtn.querySelector('ion-icon');
    const btuPopup = document.getElementById('btuPopup');

    const toolsAndMaterialsSelect = document.getElementById('toolsAndMaterials');
    const selectedToolsAndMaterialsContainer = document.getElementById('selectedToolsAndMaterialsContainer');

    function togglePopup() {
        const isHidden = btuPopup.classList.contains('hidden');
        btuPopup.classList.toggle('hidden');
        popupIcon.setAttribute('name', isHidden ? 'close-outline' : 'calculator-outline');
        openPopupBtn.style.backgroundColor = isHidden ? '#dc3545' : '#007bff';
    }

    function closePopup() {
        btuPopup.classList.add('hidden');
        popupIcon.setAttribute('name', 'calculator-outline');
        openPopupBtn.style.backgroundColor = '#007bff';
    }

    openPopupBtn.addEventListener('click', togglePopup);

    btuPopup.addEventListener('click', (e) => {
        if (e.target === btuPopup) closePopup();
    });

    function setupMultiSelect(selectElement, containerElement) {
        function updateTags() {
            containerElement.innerHTML = '';
            Array.from(selectElement.selectedOptions).forEach(option => {
                const tag = createTag(option, updateTags);
                containerElement.appendChild(tag);
            });
        }

        function toggleOption(e) {
            e.preventDefault();
            if (e.target.tagName === 'OPTION') {
                e.target.selected = !e.target.selected;
                updateTags();
            }
        }

        selectElement.addEventListener('mousedown', toggleOption);
        updateTags();
    }

    function createTag(option, updateTagsCallback) {
        const tag = document.createElement('div');
        tag.classList.add('tool-tag');
        tag.textContent = option.textContent;

        const removeBtn = document.createElement('span');
        removeBtn.classList.add('remove-tool');
        removeBtn.textContent = '×';
        removeBtn.addEventListener('click', () => {
            option.selected = false;
            updateTagsCallback();
        });

        tag.appendChild(removeBtn);
        return tag;
    }

    setupMultiSelect(toolsAndMaterialsSelect, selectedToolsAndMaterialsContainer);
});
