document.addEventListener('DOMContentLoaded', () => {
    const warehouseId = document.querySelector('.page-container').dataset.warehouseId;
    const equipmentGrid = document.querySelector('[data-category="equipment"]');
    const materialsGrid = document.querySelector('[data-category="materials"]');
    const toolsGrid = document.querySelector('[data-category="tools"]');
    let currentCard = null;
    let deleteTargetCard = null;

    async function loadAll() {
        await Promise.all([
            loadEquipment(),
            loadMaterials(),
            loadTools()
        ]);
    }

    async function loadEquipment() {
        try {
            const res = await fetch(`/Warehouses/GetEquipment?warehouseId=${warehouseId}`);
            const data = await res.json();
            equipmentGrid.innerHTML = '';
            data.forEach(renderEquipmentCard);
        } catch (err) {
            console.error('Failed to load equipment:', err);
        }
    }

    async function loadMaterials() {
        try {
            const res = await fetch(`/Warehouses/GetMaterials?warehouseId=${warehouseId}`);
            const data = await res.json();
            materialsGrid.innerHTML = '';
            data.forEach(renderMaterialCard);
        } catch (err) {
            console.error('Failed to load materials:', err);
        }
    }

    async function loadTools() {
        try {
            const res = await fetch(`/Warehouses/GetTools?warehouseId=${warehouseId}`);
            const data = await res.json();
            toolsGrid.innerHTML = '';
            data.forEach(renderToolCard);
        } catch (err) {
            console.error('Failed to load tools:', err);
        }
    }

    function renderEquipmentCard(item) {
        const card = document.createElement('div');
        card.className = 'item-card';
        card.dataset.id = item.id;
        card.innerHTML = `
            <h3>Model: ${item.modelName}</h3>
            <p>BTU: ${item.btu}</p>
            <p>Covers: ${item.serviceArea} m²</p>
            <p>Price: ${item.price} MDL</p>
            <div class="stock-control">
                <span>In stock:</span>
                <button class="stock-btn decrement">−</button>
                <span class="stock-amount">${item.quantity}</span>
                <button class="stock-btn increment">+</button>
            </div>
            <div class="card-actions">
                <button class="edit-btn">✏️ Edit</button>
                <button class="delete-btn">Delete</button>
            </div>
        `;
        equipmentGrid.appendChild(card);
    }

    function renderMaterialCard(item) {
        const card = document.createElement('div');
        card.className = 'item-card';
        card.dataset.id = item.id;
        card.innerHTML = `
            <h3>${item.materialName}</h3>
            <p class="material-price">Price: ${item.price} MDL</p>
            <div class="stock-control">
                <span>Quantity:</span>
                <button class="stock-btn decrement">−</button>
                <span class="stock-amount">${item.quantity}</span>
                <button class="stock-btn increment">+</button>
            </div>
            <div class="card-actions">
                <button class="edit-btn">✏️ Edit</button>
                <button class="delete-btn">Delete</button>
            </div>
        `;
        materialsGrid.appendChild(card);
    }

    function renderToolCard(item) {
        const card = document.createElement('div');
        card.className = 'item-card';
        card.dataset.id = item.id;
        card.innerHTML = `
            <h3>${item.toolName}</h3>
            <div class="stock-control">
                <span>Quantity:</span>
                <button class="stock-btn decrement">−</button>
                <span class="stock-amount">${item.quantity}</span>
                <button class="stock-btn increment">+</button>
            </div>
            <div class="card-actions">
                <button class="edit-btn">✏️ Edit</button>
                <button class="delete-btn">Delete</button>
            </div>
        `;
        toolsGrid.appendChild(card);
    }

    window.saveNewEquipment = async function () {
        const name = document.getElementById('newEquipName').value.trim();
        const btu = parseInt(document.getElementById('newEquipBTU').value);
        const area = parseInt(document.getElementById('newEquipArea').value);
        const price = parseFloat(document.getElementById('newEquipPrice').value);
        const qty = parseInt(document.getElementById('newEquipQty').value);

        const payload = {
            warehouseId,
            modelName: name,
            btu,
            serviceArea: area,
            price,
            quantity: qty
        };

        try {
            const res = await fetch(`/Warehouses/AddEquipment`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                closeModal('modalAddEquipment');
                clearModalInputs('modalAddEquipment');
                await loadAll();
            } else {
                const err = await res.json();

                if (err.errors) {
                    const messages = Object.values(err.errors).flat().join('\n');
                    alert(`Validation errors:\n${messages}`);
                } else {
                    alert(`Error: ${err.message || 'Failed to add equipment'}`);
                }
            }
        } catch (error) {
            console.error('Failed to add equipment:', error);
            alert('An error occurred while sending the request.');
        }
    };

    window.saveNewMaterial = async function () {
        const name = document.getElementById('newMatName').value.trim();
        const qty = parseInt(document.getElementById('newMatQty').value);
        const price = parseFloat(document.getElementById('newMatPrice').value);

        const payload = {
            warehouseId,
            materialName: name,
            quantity: qty,
            price
        };

        try {
            const res = await fetch(`/Warehouses/AddMaterial`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                closeModal('modalAddMaterial');
                clearModalInputs('modalAddMaterial');
                await loadAll();
            } else {
                const err = await res.json();
                const errors = err.errors
                    ? Object.values(err.errors).flat().join('\n')
                    : err.message || 'Failed to add material';
                alert(`Error:\n${errors}`);
            }
        } catch (error) {
            console.error('Error adding material:', error);
            alert('An error occurred while sending the request.');
        }
    };

    window.saveNewTool = async function () {
        const name = document.getElementById('newToolName').value.trim();
        const qty = parseInt(document.getElementById('newToolQty').value);

        const payload = {
            warehouseId,
            toolName: name,
            quantity: qty
        };

        try {
            const res = await fetch(`/Warehouses/AddTool`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                closeModal('modalAddTool');
                clearModalInputs('modalAddTool');
                await loadAll();
            } else {
                const err = await res.json();
                const errors = err.errors
                    ? Object.values(err.errors).flat().join('\n')
                    : err.message || 'Failed to add tool';
                alert(`Error:\n${errors}`);
            }
        } catch (error) {
            console.error('Failed to add tool:', error);
            alert('An error occurred while sending the request.');
        }
    };

    document.getElementById('confirmDeleteBtn').addEventListener('click', async () => {
        if (!deleteTargetCard || !deleteTargetCard.dataset || !deleteTargetCard.dataset.id) {
            closeModal('deleteConfirmModal');
            return;
        }

        const id = deleteTargetCard.dataset.id;
        const section = deleteTargetCard.closest('.category-section');
        const category = section.querySelector('.toggle-header')?.textContent.trim() || '';

        let url = '';
        if (category.includes('Equipment')) {
            url = `/Warehouses/DeleteEquipment?id=${id}`;
        } else if (category.includes('Materials')) {
            url = `/Warehouses/DeleteMaterial?id=${id}`;
        } else if (category.includes('Tools')) {
            url = `/Warehouses/DeleteTool?id=${id}`;
        }

        try {
            const res = await fetch(url, { method: 'DELETE' });
            if (res.ok) {
                deleteTargetCard.remove();
                deleteTargetCard = null;
                closeModal('deleteConfirmModal');
            } else {
                const err = await res.json();
                alert(`Error deleting: ${err.message}`);
            }
        } catch (error) {
            console.error('Error deleting:', error);
            alert('An error occurred while deleting item.');
        }
    });

    window.saveEquipment = async function () {
        if (!currentCard) return;
        const id = currentCard.dataset.id;

        const existingName = currentCard.querySelector('h3')?.textContent.replace('Model: ', '') || '';
        const existingBTU = parseInt(currentCard.querySelector('p:nth-of-type(1)')?.textContent.replace('BTU: ', '') || 0);
        const existingArea = parseInt(currentCard.querySelector('p:nth-of-type(2)')?.textContent.replace('Covers: ', '') || 0);
        const existingPrice = parseFloat(currentCard.querySelector('p:nth-of-type(3)')?.textContent.replace('Price: ', '').replace('MDL', '') || 0);
        const existingQty = parseInt(currentCard.querySelector('.stock-amount')?.textContent || 0);

        const name = document.getElementById('equipName').value.trim();
        const btu = parseInt(document.getElementById('equipBTU').value);
        const area = parseInt(document.getElementById('equipArea').value);
        const price = parseFloat(document.getElementById('equipPrice').value);
        const quantity = parseInt(document.getElementById('equipQty').value);

        const unchanged =
            existingName === name &&
            existingBTU === btu &&
            existingArea === area &&
            existingPrice === price &&
            existingQty === quantity;

        if (unchanged) {
            closeModal('modalEquipment');
            return;
        }

        const payload = { id, warehouseId, modelName: name, btu, serviceArea: area, price, quantity };

        try {
            const res = await fetch(`/Warehouses/UpdateEquipment?id=${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                closeModal('modalEquipment');
                await loadAll();
            } else {
                const err = await res.json();
                alert(`Failed to update equipment: ${err.message}`);
            }
        } catch (error) {
            console.error('Error updating equipment:', error);
        }
    };

    window.saveMaterial = async function () {
        if (!currentCard) return;
        const id = currentCard.dataset.id;

        const existingName = currentCard.querySelector('h3')?.textContent || '';
        const existingPrice = parseFloat(currentCard.querySelector('.material-price')?.textContent.replace('Price: ', '').replace('MDL', '') || 0);
        const existingQty = parseInt(currentCard.querySelector('.stock-amount')?.textContent || 0);

        const name = document.getElementById('matName').value.trim();
        const price = parseFloat(document.getElementById('matPrice').value);
        const quantity = parseInt(document.getElementById('matQty').value);

        const unchanged =
            existingName === name &&
            existingPrice === price &&
            existingQty === quantity;

        if (unchanged) {
            closeModal('modalMaterial');
            return;
        }

        const payload = { id, warehouseId, materialName: name, price, quantity };

        try {
            const res = await fetch(`/Warehouses/UpdateMaterial?id=${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                closeModal('modalMaterial');
                await loadAll();
            } else {
                const err = await res.json();
                alert(`Failed to update material: ${err.message}`);
            }
        } catch (error) {
            console.error('Error updating material:', error);
        }
    };

    window.saveTool = async function () {
        if (!currentCard) return;
        const id = currentCard.dataset.id;

        const existingName = currentCard.querySelector('h3')?.textContent || '';
        const existingQty = parseInt(currentCard.querySelector('.stock-amount')?.textContent || 0);

        const name = document.getElementById('toolName').value.trim();
        const quantity = parseInt(document.getElementById('toolQty').value);

        const unchanged =
            existingName === name &&
            existingQty === quantity;

        if (unchanged) {
            closeModal('modalTool');
            return;
        }

        const payload = { id, warehouseId, toolName: name, quantity };

        try {
            const res = await fetch(`/Warehouses/UpdateTool?id=${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                closeModal('modalTool');
                await loadAll();
            } else {
                const err = await res.json();
                alert(`Failed to update tool: ${err.message}`);
            }
        } catch (error) {
            console.error('Error updating tool:', error);
        }
    };

    document.addEventListener('click', async function (e) {
        if (e.target.classList.contains('increment') || e.target.classList.contains('decrement')) {
            const card = e.target.closest('.item-card');
            const amountEl = card.querySelector('.stock-amount');
            const id = card.dataset.id;
            const section = card.closest('.category-section');
            const categoryText = section.querySelector('.toggle-header')?.textContent.trim().toLowerCase() || '';

            let categoryType = '';
            if (categoryText.includes('equipment')) categoryType = 'equipment';
            else if (categoryText.includes('materials')) categoryType = 'materials';
            else if (categoryText.includes('tools')) categoryType = 'tools';

            const baseUrlMap = {
                equipment: 'UpdateEquipment',
                materials: 'UpdateMaterial',
                tools: 'UpdateTool'
            };

            const baseUrl = baseUrlMap[categoryType];
            if (!baseUrl) return;

            let amount = parseInt(amountEl.textContent);
            if (isNaN(amount)) amount = 0;

            amount += e.target.classList.contains('increment') ? 1 : -1;
            amount = Math.max(0, amount);

            const payload = { id, warehouseId, quantity: amount };

            if (categoryType === 'equipment') {
                payload.modelName = card.querySelector('h3')?.textContent.replace('Model: ', '') || '';
                payload.btu = parseInt(card.querySelector('p:nth-of-type(1)')?.textContent.replace('BTU: ', '') || 0);
                payload.serviceArea = parseInt(card.querySelector('p:nth-of-type(2)')?.textContent.replace('Covers: ', '') || 0);
                payload.price = parseFloat(card.querySelector('p:nth-of-type(3)')?.textContent.replace('Price: ', '').replace('MDL', '') || 0);
            } else if (categoryType === 'materials') {
                payload.materialName = card.querySelector('h3')?.textContent || '';
                payload.price = parseFloat(card.querySelector('.material-price')?.textContent.replace('Price: ', '').replace('MDL', '') || 0);
            } else if (categoryType === 'tools') {
                payload.toolName = card.querySelector('h3')?.textContent || '';
            }

            try {
                const res = await fetch(`/Warehouses/${baseUrl}?id=${id}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });

                if (res.ok) {
                    amountEl.textContent = amount;
                } else {
                    const err = await res.json();
                    alert(`Failed to update quantity: ${err.message}`);
                }
            } catch (error) {
                console.error('Failed to update quantity:', error);
            }
        }
    });


    document.querySelectorAll('.toggle-header').forEach(header => {
        header.addEventListener('click', () => {
            const section = header.closest('.category-section');
            const grid = section.querySelector('.item-grid');

            grid.classList.toggle('hidden');
            section.classList.toggle('collapsed');
        });
    });

    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('edit-btn')) {
            currentCard = e.target.closest('.item-card');
            const section = currentCard.closest('.category-section');
            const category = section.querySelector('.toggle-header').textContent.trim();

            if (category.includes('Equipment')) {
                document.getElementById('equipName').value = currentCard.querySelector('h3')?.textContent.replace('Model: ', '') || '';
                document.getElementById('equipBTU').value = currentCard.querySelector('p:nth-of-type(1)')?.textContent.replace('BTU: ', '') || '';
                document.getElementById('equipArea').value = currentCard.querySelector('p:nth-of-type(2)')?.textContent.replace('Covers: ', '') || '';
                document.getElementById('equipPrice').value = currentCard.querySelector('p:nth-of-type(3)')?.textContent.replace('Price: ', '').replace('MDL', '').trim() || '';
                document.getElementById('equipQty').value = currentCard.querySelector('.stock-amount')?.textContent || '';
                document.getElementById('modalEquipment').classList.remove('hidden');
            } else if (category.includes('Materials')) {
                document.getElementById('matName').value = currentCard.querySelector('h3').textContent;
                document.getElementById('matQty').value = currentCard.querySelector('.stock-amount').textContent;
                document.getElementById('matPrice').value = currentCard.querySelector('.material-price').textContent.replace('Price: ', '').replace('MDL', '').trim();
                document.getElementById('modalMaterial').classList.remove('hidden');
            } else if (category.includes('Tools')) {
                document.getElementById('toolName').value = currentCard.querySelector('h3').textContent;
                document.getElementById('toolQty').value = currentCard.querySelector('.stock-amount').textContent;
                document.getElementById('modalTool').classList.remove('hidden');
            }
        }

        if (e.target.classList.contains('delete-btn')) {
            deleteTargetCard = e.target.closest('.item-card');
            const name = deleteTargetCard?.querySelector('h3')?.textContent || 'item';
            document.getElementById('deleteConfirmText').textContent = `Are you sure you want to delete «${name}»?`;
            document.getElementById('deleteConfirmModal').classList.remove('hidden');
        }
    });

    function closeModal(id) {
        document.getElementById(id)?.classList.add('hidden');
        clearModalInputs(id);
        currentCard = null;
        deleteTargetCard = null;
    }

    document.querySelectorAll('.cancel-button').forEach(button => {
        button.addEventListener('click', () => {
            const modalId = button.dataset.modal;
            if (modalId) closeModal(modalId);
        });
    });


    /* поиск */
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.addEventListener('input', debounce(handleSearch, 200));
    }

    function handleSearch() {
        const query = this.value.toLowerCase();

        document.querySelectorAll('.item-card').forEach(card => {
            const name = card.querySelector('h3')?.textContent.toLowerCase() || '';
            const pTexts = Array.from(card.querySelectorAll('p'))
                .map(p => p.textContent.toLowerCase());

            const priceText = pTexts.find(t => t.includes('Price')) || '';
            const priceNumber = priceText.match(/\d+/g)?.join('') || '';
            const btuText = pTexts.find(t => t.includes('btu')) || '';
            const areaText = pTexts.find(t => t.includes('Covers')) || '';
            const quantity = card.querySelector('.stock-amount')?.textContent.toLowerCase() || '';

            const matches =
                name.includes(query) ||
                priceText.includes(query) ||
                priceNumber.includes(query) ||
                quantity.includes(query) ||
                btuText.includes(query) ||
                areaText.includes(query);

            card.style.display = matches ? '' : 'none';
        });
    }

    function debounce(fn, delay = 300) {
        let timeout;
        return function (...args) {
            clearTimeout(timeout);
            timeout = setTimeout(() => fn.apply(this, args), delay);
        };
    }

    function collapseAllSections() {
        document.querySelectorAll('.category-section').forEach(section => {
            section.classList.add('collapsed');
            const grid = section.querySelector('.item-grid');
            if (grid) grid.classList.add('hidden');
        });
    }

    document.querySelectorAll('.add-btn').forEach(button => {
        button.addEventListener('click', () => {
            const section = button.closest('.category-section');
            const category = section.querySelector('.toggle-header').textContent.trim().toLowerCase();

            if (category.includes('equipment')) {
                document.getElementById('modalAddEquipment').classList.remove('hidden');
            } else if (category.includes('materials')) {
                document.getElementById('modalAddMaterial').classList.remove('hidden');
            } else if (category.includes('tools')) {
                document.getElementById('modalAddTool').classList.remove('hidden');
            }
        });
    });


    function clearModalInputs(modalId) {
        const modal = document.getElementById(modalId);
        if (!modal) return;

        const inputs = modal.querySelectorAll('input');
        inputs.forEach(input => {
            input.value = '';
        });
    }



    loadAll();
    collapseAllSections();
});
