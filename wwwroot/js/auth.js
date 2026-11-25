document.addEventListener("DOMContentLoaded", function () {
    // Password show/hide toggle
    document.querySelectorAll("[data-toggle-password]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            const targetId = btn.getAttribute("data-toggle-password");
            const input = document.getElementById(targetId);
            if (!input) return;

            const isPassword = input.type === "password";
            input.type = isPassword ? "text" : "password";

            const icon = btn.querySelector("i");
            if (icon) {
                icon.classList.toggle("fa-eye", !isPassword);
                icon.classList.toggle("fa-eye-slash", isPassword);
            }

            btn.setAttribute("aria-label", isPassword ? "Hide password" : "Show password");
        });
    });

    // Live password match hint
    document.querySelectorAll("[data-match-password]").forEach(function (confirmInput) {
        const primaryId = confirmInput.getAttribute("data-match-password");
        const hintId = confirmInput.getAttribute("data-match-hint");
        const primaryInput = document.getElementById(primaryId);
        const hintEl = hintId ? document.getElementById(hintId) : null;
        if (!primaryInput || !hintEl) return;

        function updateMatchState() {
            const pwd = primaryInput.value;
            const confirmPwd = confirmInput.value;

            if (!confirmPwd) {
                hintEl.textContent = "";
                hintEl.className = "auth-match-hint";
                return;
            }

            if (pwd === confirmPwd) {
                hintEl.innerHTML = '<i class="fa-solid fa-circle-check me-1"></i>Passwords match';
                hintEl.className = "auth-match-hint is-match";
            } else {
                hintEl.innerHTML = '<i class="fa-solid fa-circle-exclamation me-1"></i>Passwords do not match';
                hintEl.className = "auth-match-hint is-mismatch";
            }
        }

        primaryInput.addEventListener("input", updateMatchState);
        confirmInput.addEventListener("input", updateMatchState);
    });

    // Restrict OTP field to digits only
    document.querySelectorAll(".auth-otp-input").forEach(function (otpInput) {
        otpInput.addEventListener("input", function () {
            this.value = this.value.replace(/\D/g, "").slice(0, 6);
        });
    });
});
