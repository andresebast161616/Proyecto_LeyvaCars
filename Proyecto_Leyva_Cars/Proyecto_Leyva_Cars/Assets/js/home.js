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

    // Inicia el carrusel automático
    showSlide(currentSlide);
    autoSlide = setInterval(nextSlide, intervalTime);
});

/* ===================================
   JAVASCRIPT - Agrégalo a /Assets/js/home.js
   =================================== */

// Carrusel de marcas infinito
document.addEventListener('DOMContentLoaded', function () {
    const track = document.getElementById('carouselTrack');

    if (!track) return;

    // Duplicar los logos múltiples veces para efecto infinito
    const slides = Array.from(track.children);
    const numSlides = slides.length;

    // Crear 4 copias completas para un scroll suave y continuo
    for (let i = 0; i < 4; i++) {
        slides.forEach(slide => {
            const clone = slide.cloneNode(true);
            track.appendChild(clone);
        });
    }

    let position = 0;
    const speed = 1; // Velocidad del movimiento (píxeles por frame)
    let isPaused = false;
    let animationId;
    let slideWidth = 280; // Ancho de cada slide (debe coincidir con CSS)

    function animate() {
        if (!isPaused) {
            position -= speed;

            // Calcular el ancho total del conjunto original
            const resetPoint = -(slideWidth * numSlides);

            // Reiniciar cuando se completa el primer conjunto
            if (position <= resetPoint) {
                position = 0;
            }

            track.style.transform = `translateX(${position}px)`;
        }
        animationId = requestAnimationFrame(animate);
    }

    // Pausar al pasar el mouse
    track.addEventListener('mouseenter', function () {
        isPaused = true;
    });

    track.addEventListener('mouseleave', function () {
        isPaused = false;
    });

    // Iniciar animación
    animate();
});