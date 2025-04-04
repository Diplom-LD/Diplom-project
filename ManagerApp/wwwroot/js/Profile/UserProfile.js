document.addEventListener('DOMContentLoaded', async () => {
    if (typeof VANTA !== 'undefined') {
        VANTA.WAVES({
            el: "#vanta-bg",
            mouseControls: true,
            touchControls: true,
            color: 0xababab,
            waveHeight: 30.00,
            shininess: 0,
            waveSpeed: 1,
            zoom: 1.02
        });
    }

    const form = document.getElementById('userProfileForm');
    const statusEl = document.getElementById('updateStatus');

    const modal = document.getElementById('confirmModal');
    const modalYes = document.getElementById('confirmYes');
    const modalNo = document.getElementById('confirmNo');

    let payload = null;

    try {
        const res = await fetch('/Profile/ViewProfile', {
            method: 'GET',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (!res.ok) throw new Error("Failed to load user profile.");

        const data = await res.json();

        document.getElementById('UserName').value = data.userName ?? '';
        document.getElementById('Email').value = data.email ?? '';
        document.getElementById('Role').value = data.role ?? '';
        document.getElementById('FirstName').value = data.firstName ?? '';
        document.getElementById('LastName').value = data.lastName ?? '';
        document.getElementById('PhoneNumber').value = data.phoneNumber ?? '';
        document.getElementById('Address').value = data.address ?? '';
    } catch (err) {
        console.error(err);
        statusEl.textContent = "⚠️ Failed to load profile.";
        statusEl.style.color = "red";
    }

    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        payload = {
            firstName: document.getElementById('FirstName').value,
            lastName: document.getElementById('LastName').value,
            phone: document.getElementById('PhoneNumber').value,
            address: document.getElementById('Address').value
        };

        modal.classList.remove('hidden');
    });

    modalYes.addEventListener('click', async () => {
        modal.classList.add('hidden');

        try {
            const res = await fetch('/Profile/UpdateProfile', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? ''
                },
                body: JSON.stringify(payload)
            });

            if (!res.ok) {
                const errText = await res.text();
                throw new Error(errText);
            }

            const result = await res.json();
            statusEl.textContent = result.message ?? "✅ Profile updated!";
            statusEl.style.color = "green";
        } catch (err) {
            console.error(err);
            statusEl.textContent = "❌ Failed to update profile.";
            statusEl.style.color = "red";
        }
    });

    modalNo.addEventListener('click', () => {
        modal.classList.add('hidden');
    });
});
