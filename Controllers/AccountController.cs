using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using CSS.Models;
using CSS.Services;
using CSS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CSS.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;

        private const string SessionOtpKey = "OTP";
        private const string SessionOtpEmailKey = "OTP_Email";
        private const string SessionOtpTimeKey = "OTP_Time";
        private const string SessionOtpVerifiedEmailKey = "OTP_Verified_Email";

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        [Authorize]
        public async Task<IActionResult> MakeMeAdmin()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));
            await _userManager.AddToRoleAsync(user, "Admin");
            return Content("You are now Admin!");
        }

        // =============================
        // GOOGLE LOGIN
        // =============================
        public IActionResult GoogleLogin(string returnUrl = "/")
        {
            var redirectUrl = Url.Action(nameof(GoogleResponse), "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return Challenge(properties, "Google");
        }

        public Task<IActionResult> GoogleResponse(string returnUrl = "/") =>
            HandleExternalLoginResponseAsync(returnUrl);

        // =============================
        // FACEBOOK LOGIN
        // =============================
        public IActionResult FacebookLogin(string returnUrl = "/")
        {
            var redirectUrl = Url.Action(nameof(FacebookResponse), "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Facebook", redirectUrl);
            return Challenge(properties, "Facebook");
        }

        public Task<IActionResult> FacebookResponse(string returnUrl = "/") =>
            HandleExternalLoginResponseAsync(returnUrl);

        private async Task<IActionResult> HandleExternalLoginResponseAsync(string returnUrl)
        {
            var safeReturnUrl = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";

            try
            {
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    TempData["AuthError"] = "External login failed or was cancelled.";
                    return RedirectToAction(nameof(Login));
                }

                var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
                if (result.Succeeded)
                    return LocalRedirect(safeReturnUrl);

                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrWhiteSpace(email))
                {
                    TempData["AuthError"] = "Could not retrieve email address from the external provider.";
                    return RedirectToAction(nameof(Login));
                }

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = info.Principal.FindFirstValue(ClaimTypes.Name),
                        MemberType = "Volunteer",
                        EmailConfirmed = true,
                        CreatedDate = DateTime.UtcNow
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        TempData["AuthError"] = "Unable to create account with external login.";
                        return RedirectToAction(nameof(Login));
                    }

                    await EnsureUserRoleAssignedAsync(user);
                }

                await _userManager.AddLoginAsync(user, info);
                await _signInManager.SignInAsync(user, isPersistent: false);

                return LocalRedirect(safeReturnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External login failed.");
                TempData["AuthError"] = "An unexpected error occurred during social login. Please try again.";
                return RedirectToAction(nameof(Login));
            }
        }

        // =============================
        // REGISTER
        // =============================
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["AuthPage"] = true;
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewData["AuthPage"] = true;

            if (!string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Password and confirmation password do not match.");
            }

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var email = model.Email!.Trim();
                var existing = await _userManager.FindByEmailAsync(email);
                if (existing != null)
                {
                    ModelState.AddModelError(nameof(model.Email), "This email address is already registered. Please use a different email or sign in.");
                    return View(model);
                }

                var bloodGroup = string.IsNullOrWhiteSpace(model.BloodGroup) ? null : model.BloodGroup.Trim();

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = model.FullName?.Trim(),
                    PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
                    BloodGroup = bloodGroup,
                    DonationEligibility = bloodGroup != null ? "Eligible" : null,
                    MemberType = "Volunteer",
                    EmailConfirmed = true,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password!);
                if (!result.Succeeded)
                {
                    foreach (var err in result.Errors)
                    {
                        if (err.Code is "DuplicateEmail" or "DuplicateUserName")
                        {
                            ModelState.AddModelError(nameof(model.Email), "This email address is already registered.");
                        }
                        else if (err.Code.StartsWith("Password", StringComparison.OrdinalIgnoreCase))
                        {
                            ModelState.AddModelError(nameof(model.Password), err.Description);
                        }
                        else
                        {
                            ModelState.AddModelError("", err.Description);
                        }
                    }

                    return View(model);
                }

                await EnsureUserRoleAssignedAsync(user);

                TempData["AuthSuccess"] = "Account created successfully! Please sign in with your email and password.";
                TempData["RegisteredEmail"] = email;

                return RedirectToAction(nameof(Login));
            }
            catch (DbUpdateException dbEx) when (IsUniqueConstraintViolation(dbEx))
            {
                _logger.LogWarning(dbEx, "Duplicate email constraint hit during registration for {Email}", model.Email);
                ModelState.AddModelError(nameof(model.Email), "This email address is already registered.");
                return View(model);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "Database error during registration for {Email}", model.Email);
                ModelState.AddModelError("", "Unable to connect to the database right now. Please try again in a moment.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for {Email}", model.Email);
                ModelState.AddModelError("", "Something went wrong while creating your account. Please try again.");
                return View(model);
            }
        }

        // =============================
        // LOGIN
        // =============================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["AuthPage"] = true;

            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl,
                Email = TempData["RegisteredEmail"] as string
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewData["AuthPage"] = true;

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var email = model.Email!.Trim();
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    ModelState.AddModelError("", "Invalid email or password. Please try again.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(
                    user, model.Password!, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                        return Redirect(model.ReturnUrl);

                    return RedirectToAction("Index", "Home");
                }

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError("", "Your account is temporarily locked. Please try again later.");
                    return View(model);
                }

                ModelState.AddModelError("", "Invalid email or password. Please try again.");
                return View(model);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "Database error during login for {Email}", model.Email);
                ModelState.AddModelError("", "Unable to connect to the database right now. Please try again in a moment.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for {Email}", model.Email);
                ModelState.AddModelError("", "An unexpected error occurred while signing in. Please try again.");
                return View(model);
            }
        }

        // =============================
        // FORGOT PASSWORD (OTP)
        // =============================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            ViewData["AuthPage"] = true;
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            ViewData["AuthPage"] = true;

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var email = model.Email!.Trim();
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    HttpContext.Session.SetString(SessionOtpEmailKey, email);
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

                HttpContext.Session.SetString(SessionOtpKey, otp);
                HttpContext.Session.SetString(SessionOtpEmailKey, email);
                HttpContext.Session.SetString(SessionOtpTimeKey, DateTime.UtcNow.ToString("O"));
                HttpContext.Session.Remove(SessionOtpVerifiedEmailKey);

                var html = $@"
                    <div style='font-family:Segoe UI,Arial,sans-serif;max-width:520px;margin:auto;padding:24px;border:1px solid #e5e7eb;border-radius:12px;'>
                        <h2 style='color:#15803D;margin-top:0;'>Conscious Student Society</h2>
                        <p>Your 6-digit password reset OTP code is:</p>
                        <div style='font-size:28px;font-weight:700;letter-spacing:6px;color:#15803D;background:#f0fdf4;padding:14px 20px;border-radius:8px;text-align:center;margin:16px 0;'>{otp}</div>
                        <p style='color:#64748b;font-size:13px;'>This code will expire in 10 minutes. If you did not request a password reset, please ignore this email.</p>
                    </div>";

                await _emailSender.SendEmailAsync(email, "CSS Password Reset OTP", html);

                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP email to {Email}", model.Email);
                ModelState.AddModelError("", "Could not send the OTP email right now. Please verify email settings or try again later.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            ViewData["AuthPage"] = true;
            return View();
        }

        // =============================
        // VERIFY OTP
        // =============================
        [HttpGet]
        public IActionResult VerifyOtp()
        {
            ViewData["AuthPage"] = true;
            var email = HttpContext.Session.GetString(SessionOtpEmailKey);
            if (string.IsNullOrEmpty(email))
                return RedirectToAction(nameof(ForgotPassword));

            return View(new OtpVerifyViewModel { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp(OtpVerifyViewModel model)
        {
            ViewData["AuthPage"] = true;

            if (!ModelState.IsValid)
                return View(model);

            var sessionOtp = HttpContext.Session.GetString(SessionOtpKey);
            var sessionEmail = HttpContext.Session.GetString(SessionOtpEmailKey);
            var sessionTimeStr = HttpContext.Session.GetString(SessionOtpTimeKey);

            if (string.IsNullOrEmpty(sessionOtp) || string.IsNullOrEmpty(sessionEmail))
            {
                ModelState.AddModelError("", "OTP session expired. Please request a new OTP.");
                return View(model);
            }

            if (DateTime.TryParse(sessionTimeStr, null, DateTimeStyles.RoundtripKind, out var issuedAt) &&
                DateTime.UtcNow - issuedAt > TimeSpan.FromMinutes(10))
            {
                ClearOtpSession();
                ModelState.AddModelError("", "Your OTP has expired. Please request a new one.");
                return View(model);
            }

            if (!string.Equals(model.OtpCode?.Trim(), sessionOtp, StringComparison.Ordinal) ||
                !string.Equals(model.Email?.Trim(), sessionEmail, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("", "Invalid OTP code. Please check and try again.");
                return View(model);
            }

            HttpContext.Session.SetString(SessionOtpVerifiedEmailKey, sessionEmail);
            return RedirectToAction(nameof(ResetPassword));
        }

        // =============================
        // RESET PASSWORD
        // =============================
        [HttpGet]
        public IActionResult ResetPassword()
        {
            ViewData["AuthPage"] = true;
            var verifiedEmail = HttpContext.Session.GetString(SessionOtpVerifiedEmailKey);
            if (string.IsNullOrEmpty(verifiedEmail))
                return RedirectToAction(nameof(ForgotPassword));

            return View(new ResetPasswordViewModel { Email = verifiedEmail });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            ViewData["AuthPage"] = true;

            var verifiedEmail = HttpContext.Session.GetString(SessionOtpVerifiedEmailKey);
            if (string.IsNullOrEmpty(verifiedEmail) ||
                !string.Equals(verifiedEmail, model.Email?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var user = await _userManager.FindByEmailAsync(verifiedEmail);
                if (user == null)
                {
                    ModelState.AddModelError("", "Account not found.");
                    return View(model);
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword!);

                if (result.Succeeded)
                {
                    ClearOtpSession();
                    return RedirectToAction(nameof(ResetPasswordConfirmation));
                }

                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for {Email}", verifiedEmail);
                ModelState.AddModelError("", "An unexpected error occurred while updating your password. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            ViewData["AuthPage"] = true;
            return View();
        }

        // =============================
        // LOGOUT
        // =============================
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        private async Task EnsureUserRoleAssignedAsync(ApplicationUser user)
        {
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                await _roleManager.CreateAsync(new IdentityRole("User"));
            }

            if (!await _userManager.IsInRoleAsync(user, "User"))
            {
                var roleResult = await _userManager.AddToRoleAsync(user, "User");
                if (!roleResult.Succeeded)
                {
                    _logger.LogWarning("Failed to assign User role to {Email}: {Errors}",
                        user.Email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627);
        }

        private void ClearOtpSession()
        {
            HttpContext.Session.Remove(SessionOtpKey);
            HttpContext.Session.Remove(SessionOtpEmailKey);
            HttpContext.Session.Remove(SessionOtpTimeKey);
            HttpContext.Session.Remove(SessionOtpVerifiedEmailKey);
        }
    }
}
