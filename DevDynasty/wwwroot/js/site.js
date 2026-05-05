document.addEventListener("DOMContentLoaded", function () {
    const toggleButton = document.getElementById("mobile-menu-toggle");
    const menu = document.getElementById("mobile-menu");
    const icon = document.getElementById("mobile-menu-icon");

    if (!toggleButton || !menu || !icon) {
        return;
    }

    const gallery = document.getElementById("gallery");

    setInterval(() => {
        gallery.scrollLeft += 300;
    }, 3000);

    toggleButton.addEventListener("click", function () {
        const isOpen = !menu.classList.contains("hidden");

        if (isOpen) {
            menu.classList.add("hidden");
            menu.classList.remove("flex");
            icon.textContent = "☰";
        } else {
            menu.classList.remove("hidden");
            menu.classList.add("flex");
            icon.textContent = "×";
        }
    });
});