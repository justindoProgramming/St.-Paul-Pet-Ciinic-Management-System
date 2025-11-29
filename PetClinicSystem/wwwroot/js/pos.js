// ==============================
// SERVICE LIST 
// ==============================
const services = [
    { name: "General Consultation", price: 300 },
    { name: "Vaccination", price: 450 },
    { name: "Dental Cleaning", price: 1500 },
    { name: "Surgery Consultation", price: 500 },
    { name: "X-Ray", price: 1200 },
    { name: "Blood Test", price: 550 },
    { name: "Ultrasound", price: 1500 },
    { name: "Deworming Tablet", price: 150 },
    { name: "Parvo Test", price: 850 },
    { name: "Heartworm Test", price: 700 },
    { name: "Grooming (Basic)", price: 350 },
    { name: "Grooming (Full)", price: 700 },
    { name: "Nail Trimming", price: 120 },
    { name: "Ear Cleaning", price: 180 },
    { name: "Spay Surgery", price: 2500 },
    { name: "Neuter Surgery", price: 2200 },
    { name: "Microchipping", price: 500 },
    { name: "Fecal Test", price: 300 },
    { name: "Allergy Test", price: 1200 },
    { name: "Fluid Therapy", price: 450 },
    { name: "Wound Treatment", price: 350 },
    { name: "Hospitalization (per day)", price: 900 },
    { name: "Rabies Vaccination", price: 300 },
    { name: "Tick & Flea Treatment", price: 400 },
    { name: "Kennel Cough Vaccination", price: 450 },
    { name: "Skin Scraping Test", price: 500 }
];

let cart = [];

// ==============================
// LOAD SERVICES INTO VIEW
// ==============================
function loadServices() {
    const container = document.getElementById("serviceList");
    container.innerHTML = "";

    services.forEach((s, index) => {
        container.innerHTML += `
            <div class="col-md-6">
                <div class="card p-3 shadow-sm rounded-4 serviceCard">
                    <div class="fw-bold">${s.name}</div>
                    <span class="badge bg-light text-dark">${s.tag || ""}</span>
                    <div class="fw-semibold mt-2">₱${s.price.toFixed(2)}</div>

                    <button class="btn btn-success btn-sm mt-3 rounded-circle addBtn"
                            onclick="addToCart(${index})">
                        +
                    </button>
                </div>
            </div>
        `;
    });
}

// ==============================
// ADD TO CART
// ==============================
function addToCart(i) {
    let item = services[i];
    let exists = cart.find(c => c.name === item.name);

    if (exists) {
        exists.qty++;
    } else {
        cart.push({ ...item, qty: 1 });
    }

    renderCart();
}

// ==============================
// RENDER CART
// ==============================
function renderCart() {
    const container = document.getElementById("cartItems");
    container.innerHTML = "";

    if (cart.length === 0) {
        document.getElementById("payButton").disabled = true;
        return;
    }

    document.getElementById("payButton").disabled = false;

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

                <div class="text-end fw-semibold mt-1">₱${(item.price * item.qty).toFixed(2)}</div>
            </div>
        `;
    });

    updateTotals();
}

// ==============================
// CHANGE QUANTITY
// ==============================
function changeQty(i, amount) {
    cart[i].qty += amount;

    if (cart[i].qty <= 0) cart.splice(i, 1);

    renderCart();
}

// ==============================
// REMOVE ITEM
// ==============================
function removeItem(i) {
    cart.splice(i, 1);
    renderCart();
}

// ==============================
// TOTALS
// ==============================
function updateTotals() {
    let subtotal = cart.reduce((sum, i) => sum + i.price * i.qty, 0);
    let tax = subtotal * 0.08; // keep your existing rate
    let total = subtotal + tax;

    document.getElementById("subtotal").innerText = `₱${subtotal.toFixed(2)}`;
    document.getElementById("tax").innerText = `₱${tax.toFixed(2)}`;
    document.getElementById("total").innerText = `₱${total.toFixed(2)}`;
}

// ==============================
// SEARCH FILTER
// ==============================
document.getElementById("searchServices").addEventListener("input", function () {
    const query = this.value.toLowerCase();

    let filtered = services.filter(s =>
        s.name.toLowerCase().includes(query) ||
        (s.tag || "").toLowerCase().includes(query)
    );

    const container = document.getElementById("serviceList");
    container.innerHTML = "";

    filtered.forEach((s, index) => {
        container.innerHTML += `
            <div class="col-md-6">
                <div class="card p-3 shadow-sm rounded-4 serviceCard">
                    <div class="fw-bold">${s.name}</div>
                    <span class="badge bg-light text-dark">${s.tag || ""}</span>
                    <div class="fw-semibold mt-2">₱${s.price.toFixed(2)}</div>

                    <button class="btn btn-success btn-sm mt-3 rounded-circle addBtn"
                            onclick="addToCart(${services.indexOf(s)})">
                        +
                    </button>
                </div>
            </div>
        `;
    });
});

// INITIAL LOAD
loadServices();
