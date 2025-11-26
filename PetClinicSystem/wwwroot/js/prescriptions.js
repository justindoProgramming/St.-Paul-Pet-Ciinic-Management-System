// Refresh prescriptions list into the prescriptions tab
function loadPrescriptions() {
    $("#prescriptionsList").load("/BillingPharmacy/PrescriptionList");
}

// ===== OPEN MODALS =====
function loadPrescriptionCreate() {
    $("#prescCreateContent").load("/BillingPharmacy/CreatePrescription", function (response, status, xhr) {
        if (status === "error") {
            if (typeof showBpToast === "function") {
                showBpToast("Error loading create form: " + (xhr.statusText || ""), "error");
            } else {
                alert("Error loading create form: " + (xhr.statusText || ""));
            }
        } else {
            $("#prescCreateModal").modal("show");
        }
    });
}

function loadPrescriptionView(id) {
    $("#prescViewContent").load("/BillingPharmacy/ViewPrescription/" + id, function (response, status, xhr) {
        if (status === "error") {
            if (typeof showBpToast === "function") {
                showBpToast("Error loading prescription details: " + (xhr.statusText || ""), "error");
            } else {
                alert("Error loading prescription details: " + (xhr.statusText || ""));
            }
        } else {
            $("#prescViewModal").modal("show");
        }
    });
}

function loadPrescriptionEdit(id) {
    $("#prescEditContent").load("/BillingPharmacy/EditPrescription/" + id, function (response, status, xhr) {
        if (status === "error") {
            if (typeof showBpToast === "function") {
                showBpToast("Error loading edit form: " + (xhr.statusText || ""), "error");
            } else {
                alert("Error loading edit form: " + (xhr.statusText || ""));
            }
        } else {
            $("#prescEditModal").modal("show");
        }
    });
}

function loadPrescriptionDelete(id) {
    $("#prescDeleteContent").load("/BillingPharmacy/DeletePrescription/" + id, function (response, status, xhr) {
        if (status === "error") {
            if (typeof showBpToast === "function") {
                showBpToast("Error loading delete confirmation: " + (xhr.statusText || ""), "error");
            } else {
                alert("Error loading delete confirmation: " + (xhr.statusText || ""));
            }
        } else {
            $("#prescDeleteModal").modal("show");
        }
    });
}

// ===== AJAX FORM SUBMIT =====
$(document).on("submit", "#createPrescriptionForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function (res) {
            $("#prescCreateModal").modal("hide");
            loadPrescriptions();

            if (typeof showBpToast === "function") {
                showBpToast(res && res.message ? res.message : "Prescription created successfully.", "success");
            } else {
                alert(res && res.message ? res.message : "Prescription created successfully.");
            }
        },
        error: function (xhr) {
            if (typeof showBpToast === "function") {
                showBpToast(xhr.responseText || "Error creating prescription.", "error");
            } else {
                alert(xhr.responseText || "Error creating prescription.");
            }
        }
    });
});

$(document).on("submit", "#editPrescriptionForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function (res) {
            $("#prescEditModal").modal("hide");
            loadPrescriptions();

            if (typeof showBpToast === "function") {
                showBpToast(res && res.message ? res.message : "Prescription updated successfully.", "success");
            } else {
                alert(res && res.message ? res.message : "Prescription updated successfully.");
            }
        },
        error: function (xhr) {
            if (typeof showBpToast === "function") {
                showBpToast(xhr.responseText || "Error updating prescription.", "error");
            } else {
                alert(xhr.responseText || "Error updating prescription.");
            }
        }
    });
});

$(document).on("submit", "#deletePrescriptionForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function (res) {
            $("#prescDeleteModal").modal("hide");
            loadPrescriptions();

            if (typeof showBpToast === "function") {
                showBpToast(res && res.message ? res.message : "Prescription deleted.", "success");
            } else {
                alert(res && res.message ? res.message : "Prescription deleted.");
            }
        },
        error: function (xhr) {
            if (typeof showBpToast === "function") {
                showBpToast(xhr.responseText || "Error deleting prescription.", "error");
            } else {
                alert(xhr.responseText || "Error deleting prescription.");
            }
        }
    });
});

// Optional: simple client-side filter
function filterPrescriptions(term) {
    term = term.toLowerCase();
    $("#prescriptionsTable tbody tr").each(function () {
        const text = $(this).text().toLowerCase();
        $(this).toggle(text.indexOf(term) !== -1);
    });
}
