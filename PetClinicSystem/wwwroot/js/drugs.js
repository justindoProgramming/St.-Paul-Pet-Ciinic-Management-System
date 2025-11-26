// Load drugs list into the inventory tab container
function loadDrugs() {
    $("#inventoryContainer").load("/BillingPharmacy/DrugList");
}

// ========== OPEN MODALS (GET PARTIALS) ==========
function loadDrugCreate() {
    $("#drugCreateContent").load("/BillingPharmacy/CreateDrug", function () {
        $("#drugCreateModal").modal("show");
    });
}

function loadDrugEdit(id) {
    $("#drugEditContent").load("/BillingPharmacy/EditDrug/" + id, function () {
        $("#drugEditModal").modal("show");
    });
}

function loadDrugDelete(id) {
    $("#drugDeleteContent").load("/BillingPharmacy/DeleteDrug/" + id, function () {
        $("#drugDeleteModal").modal("show");
    });
}

// ========== HANDLE FORM SUBMIT (AJAX) ==========
$(document).on("submit", "#createDrugForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function () {
            $("#drugCreateModal").modal("hide");
            loadDrugs();
        },
        error: function (xhr) {
            alert(xhr.responseText || "Error creating drug");
        }
    });
});

$(document).on("submit", "#editDrugForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function () {
            $("#drugEditModal").modal("hide");
            loadDrugs();
        },
        error: function (xhr) {
            alert(xhr.responseText || "Error updating drug");
        }
    });
});

$(document).on("submit", "#deleteDrugForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function () {
            $("#drugDeleteModal").modal("hide");
            loadDrugs();
        },
        error: function (xhr) {
            alert(xhr.responseText || "Error deleting drug");
        }
    });
});

// ========== CLIENT-SIDE FILTER ==========
function filterDrugs(term) {
    term = term.toLowerCase();
    $("#drugsTable tbody tr").each(function () {
        const text = $(this).text().toLowerCase();
        $(this).toggle(text.indexOf(term) !== -1);
    });
}
