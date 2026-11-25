// ================= PAYMENT =================
function payNow() {
    if (!validateForm()) return;
    document.getElementById("payMethod").value = "aamarpay";
    document.getElementById("regForm").submit();
}

// ================= FORM VALIDATION =================
function validateForm() {
    var fullNameInput = document.querySelector('#regForm input[name="FullName"]');
    var mobileInput = document.querySelector('#regForm input[name="Mobile"]');
    var emailInput = document.querySelector('#regForm input[name="Email"]');

    var fullName = fullNameInput ? fullNameInput.value.trim() : "";
    var mobile = mobileInput ? mobileInput.value.trim() : "";
    var email = emailInput ? emailInput.value.trim() : "";

    if (fullName.length < 2) {
        if (typeof Swal !== "undefined") {
            Swal.fire({ icon: "warning", title: "Invalid Name", text: "Please enter your full name.", confirmButtonColor: "#15803D" });
        } else {
            alert("Please enter your full name.");
        }
        return false;
    }

    // Bangladesh mobile format: 01XXXXXXXXX
    var mobileRegex = /^01[0-9]{9}$/;
    if (!mobileRegex.test(mobile)) {
        if (typeof Swal !== "undefined") {
            Swal.fire({ icon: "warning", title: "Invalid Mobile Number", text: "Please enter a valid 11-digit Bangladesh mobile number (starts with 01).", confirmButtonColor: "#15803D" });
        } else {
            alert("Please enter a valid Bangladesh mobile number (11 digits, starts with 01).");
        }
        return false;
    }

    // Email format (optional)
    if (email.length > 0) {
        var emailRegex = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
        if (!emailRegex.test(email)) {
            if (typeof Swal !== "undefined") {
                Swal.fire({ icon: "warning", title: "Invalid Email", text: "Please enter a valid email address.", confirmButtonColor: "#15803D" });
            } else {
                alert("Invalid email format.");
            }
            return false;
        }
    }

    return true;
}

// ================= AUTO-DISMISS SUCCESS MESSAGE =================
document.addEventListener("DOMContentLoaded", function () {
    var alertEl = document.getElementById("regSuccessAlert");
    if (!alertEl) return;

    setTimeout(function () {
        alertEl.scrollIntoView({ behavior: "smooth", block: "center" });
    }, 150);

    setTimeout(function () {
        try {
            var bsAlert = bootstrap.Alert.getOrCreateInstance(alertEl);
            bsAlert.close();
        } catch (e) {
            alertEl.remove();
        }
    }, 5000);
});
