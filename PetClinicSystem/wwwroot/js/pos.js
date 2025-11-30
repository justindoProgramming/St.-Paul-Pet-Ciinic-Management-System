// wwwroot/js/pos.js

// ==============================
// 1. SERVICES (HARDCODED LIST)
// ==============================
const services = [
    { name: "General Consultation", price: 300, tag: "Consultation" },
    { name: "Vaccination", price: 450, tag: "Vaccination" },
    { name: "Dental Cleaning", price: 1500, tag: "Dental" },
    { name: "Surgery Consultation", price: 500, tag: "Surgery" },
    { name: "X-Ray", price: 1200, tag: "Diagnostics" },
    { name: "Blood Test", price: 550, tag: "Diagnostics" },
    { name: "Ultrasound", price: 1500, tag: "Diagnostics" },
    { name: "Deworming Tablet", price: 150, tag: "Medication" },
    { name: "Parvo Test", price: 850, tag: "Diagnostics" },
    { name: "Heartworm Test", price: 700, tag: "Diagnostics" },
    { name: "Grooming (Basic)", price: 350, tag: "Grooming" },
    { name: "Grooming (Full)", price: 700, tag: "Grooming" },
    { name: "Nail Trimming", price: 120, tag: "Grooming" },
    { name: "Ear Cleaning", price: 180, tag: "Grooming" },
    { name: "Spay Surgery", price: 2500, tag: "Surgery" },
    { name: "Neuter Surgery", price: 2200, tag: "Surgery" },
    { name: "Microchipping", price: 500, tag: "Procedure" },
    { name: "Fecal Test", price: 300, tag: "Diagnostics" },
    { name: "Allergy Test", price: 1200, tag: "Diagnostics" },
    { name: "Fluid Therapy", price: 450, tag: "Treatment" },
    { name: "Wound Treatment", price: 350, tag: "Treatment" },
    { name: "Hospitalization (per day)", price: 900, tag: "Hospitalization" },
    { name: "Rabies Vaccination", price: 300, tag: "Vaccination" },
    { name: "Tick & Flea Treatment", price: 400, tag: "Treatment" },
    { name: "Kennel Cough Vaccination", price: 450, tag: "Vaccination" },
    { name: "Skin Scraping Test", price: 500, tag: "Diagnostics" }
];

// Current sale cart
let cart = [];

// ==============================
// 2. LOAD SERVICES INTO GRID
// ==============================
function loadServices(list) {
    const container = document.getElementById("serviceList");
    if (!container) return;

    const src = list || services;
    container.innerHTML = "";

    src.forEach((s, index) => {
        container.innerHTML += `
            <div class="col-md-6 mb-3">
                <div class="card h-100 p-3 shadow-sm rounded-4 d-flex justify-content-between">
                    <div>
                        <h6 class="fw-bold mb-1">${s.name}</h6>
                        <div class="text-secondary small mb-2">${s.tag || ""}</div>
                        <div class="fw-semibold">₱${s.price.toFixed(2)}</div>
                    </div>
                    <div class="text-end">
                        <button class="btn btn-success rounded-circle"
                                style="width:38px;height:38px;"
                                onclick="addToCart(${index})">
                            <i class="fa fa-plus"></i>
                        </button>
                    </div>
                </div>
            </div>
        `;
    });
}

// ==============================
// 3. CART OPERATIONS
// ==============================
function addToCart(index) {
    const item = services[index];
    if (!item) return;

    const existing = cart.find(c => c.name === item.name);
    if (existing) {
        existing.qty += 1;
    } else {
        cart.push({ ...item, qty: 1 });
    }

    renderCart();
}

function changeQty(i, amount) {
    if (!cart[i]) return;
    cart[i].qty += amount;
    if (cart[i].qty <= 0) {
        cart.splice(i, 1);
    }
    renderCart();
}

function removeItem(i) {
    cart.splice(i, 1);
    renderCart();
}

function renderCart() {
    const container = document.getElementById("cartItems");
    const payBtn = document.getElementById("payButton");
    if (!container) return;

    container.innerHTML = "";

    if (cart.length === 0) {
        if (payBtn) payBtn.disabled = true;
        updateTotals();
        return;
    }

    if (payBtn) payBtn.disabled = false;

    cart.forEach((item, index) => {
        container.innerHTML += `
            <div class="cartItem row align-items-center mb-3">
                <div class="col-6">
                    <strong>${item.name}</strong><br>
                    <span class="text-secondary small">${item.tag || ""}</span>
                </div>

                <div class="col-4 d-flex align-items-center">
                    <button class="btn btn-sm btn-outline-secondary"
                            onclick="changeQty(${index}, -1)">-</button>

                    <span class="mx-2">${item.qty}</span>

                    <button class="btn btn-sm btn-outline-secondary"
                            onclick="changeQty(${index}, 1)">+</button>
                </div>

                <div class="col-2 text-end">
                    <span class="text-danger" style="cursor:pointer;"
                          onclick="removeItem(${index})">
                        <i class="fa fa-trash"></i>
                    </span>
                </div>

                <div class="text-end fw-semibold mt-1">
                    ₱${(item.price * item.qty).toFixed(2)}
                </div>
            </div>
        `;
    });

    updateTotals();
}

// ==============================
// 4. TOTALS (₱ + 8% TAX)
// ==============================
function updateTotals() {
    const subtotal = cart.reduce((sum, i) => sum + i.price * i.qty, 0);
    const tax = subtotal * 0.08;
    const total = subtotal + tax;

    const subtotalEl = document.getElementById("subtotal");
    const taxEl = document.getElementById("tax");
    const totalEl = document.getElementById("total");

    if (subtotalEl) subtotalEl.innerText = `₱${subtotal.toFixed(2)}`;
    if (taxEl) taxEl.innerText = `₱${tax.toFixed(2)}`;
    if (totalEl) totalEl.innerText = `₱${total.toFixed(2)}`;
}

// ==============================
// 5. SEARCH FILTER
// ==============================
function wireSearch() {
    const searchInput = document.getElementById("searchServices");
    if (!searchInput) return;

    searchInput.addEventListener("input", function () {
        const query = (this.value || "").toLowerCase();

        const filtered = services.filter(s =>
            s.name.toLowerCase().includes(query) ||
            (s.tag && s.tag.toLowerCase().includes(query))
        );

        loadServices(filtered);
    });
}

// ==============================
// 6. PROCESS PAYMENT (checkout)
// ==============================
// IMPORTANT: BillingPharmacyController.SaveTransaction expects List<Billing>
// so we send PascalCase property names: PetId, StaffId, ServiceName, etc.
function checkout() {
    const petSelect = document.getElementById("petSelect");
    const petId = petSelect ? petSelect.value : "";

    if (!petId) {
        showPosToast("Please select a patient before processing payment.", "error");
        return;
    }

    if (cart.length === 0) {
        showPosToast("Cart is empty. Add at least one service.", "error");
        return;
    }

    // TODO: replace this with the actual logged-in staff id from your layout/session
    const staffId = 1;

    const items = cart.map(c => ({
        PetId: parseInt(petId),
        StaffId: staffId,
        ServiceName: c.name,
        ServicePrice: c.price,
        Quantity: c.qty,
        Total: c.price * c.qty
    }));

    fetch("/BillingPharmacy/SaveTransaction", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(items)
    })
        .then(async (res) => {
            if (!res.ok) {
                const text = await res.text();
                throw new Error(text || "Server error while saving transaction.");
            }
            return res.json();
        })
        .then((data) => {
            const msg = (data && data.message) ? data.message : "Payment saved successfully.";
            showPosToast(msg, "success");

            cart = [];
            renderCart();
        })
        .catch((err) => {
            console.error(err);
            showPosToast("Error processing payment. Please try again.", "error");
        });
}

// ==============================
// 7. TOAST HELPER
// ==============================
function showPosToast(message, type) {
    // Uses your global admin toast if available
    if (window.showAppToast) {
        window.showAppToast(message, type === "error" ? "error" : "success");
    } else if (window.showBpToast) {
        window.showBpToast(message, type === "error" ? "error" : "success");
    } else {
        alert(message);
    }
}

// ==============================
// 8. INITIALIZE ON PAGE LOAD
// ==============================
document.addEventListener("DOMContentLoaded", function () {
    loadServices();
    renderCart();
    wireSearch();
});
