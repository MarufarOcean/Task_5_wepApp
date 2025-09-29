<script>
    document.getElementById("togglePassword").addEventListener("click", function () {
        const pwd = document.getElementById("password");
    const icon = this.querySelector("i");
    if (pwd.type === "password") {
        pwd.type = "text";
    icon.classList.remove("bi-eye");
    icon.classList.add("bi-eye-slash");
        } else {
        pwd.type = "password";
    icon.classList.remove("bi-eye-slash");
    icon.classList.add("bi-eye");
        }
    });
</script>

