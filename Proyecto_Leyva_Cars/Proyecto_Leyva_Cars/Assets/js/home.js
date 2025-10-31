document.addEventListener("DOMContentLoaded", () => {
    const slides = document.querySelectorAll(".carousel-slide");
    const indicators = document.querySelectorAll(".carousel-indicator");
    const nextBtn = document.getElementById("nextSlide");
    const prevBtn = document.getElementById("prevSlide");
    let currentSlide = 0;
    const intervalTime = 3000; // 3 segundos
    let autoSlide;

    function showSlide(index) {
        slides.forEach((slide, i) => {
            slide.classList.toggle("opacity-0", i !== index);
            slide.classList.toggle("active", i === index);
        });

        indicators.forEach((dot, i) => {
            dot.classList.toggle("bg-white", i === index);
            dot.classList.toggle("bg-white/50", i !== index);
        });

        currentSlide = index;
    }

    function nextSlide() {
        const next = (currentSlide + 1) % slides.length;
        showSlide(next);
    }

    function prevSlideFunc() {
        const prev = (currentSlide - 1 + slides.length) % slides.length;
        showSlide(prev);
    }

    function resetAutoSlide() {
        clearInterval(autoSlide);
        autoSlide = setInterval(nextSlide, intervalTime);
    }

    // Eventos de botones
    if (nextBtn && prevBtn) {
        nextBtn.addEventListener("click", () => {
            nextSlide();
            resetAutoSlide();
        });

        prevBtn.addEventListener("click", () => {
            prevSlideFunc();
            resetAutoSlide();
        });
    }

    // Eventos en indicadores (dots)
    indicators.forEach((dot, i) => {
        dot.addEventListener("click", () => {
            showSlide(i);
            resetAutoSlide();
        });
    });

    // Inicia el carrusel autom�tico
    showSlide(currentSlide);
    autoSlide = setInterval(nextSlide, intervalTime);
});