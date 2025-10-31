// brand-carousel.js

document.addEventListener('DOMContentLoaded', function () {
    const track = document.getElementById('brandCarouselTrack');

    if (!track) return;

    // Clonar las imágenes múltiples veces para un efecto más suave
    const images = track.querySelectorAll('.brand-carousel-img');
    const numClones = 3; // Número de veces que se repiten los logos

    // Crear múltiples copias de los logos
    for (let i = 0; i < numClones; i++) {
        images.forEach(img => {
            const clone = img.cloneNode(true);
            track.appendChild(clone);
        });
    }

    // Velocidad del carrusel
    let position = 0;
    const speed = 0.5; // píxeles por frame

    // Calcular el ancho de un set completo de imágenes
    const singleSetWidth = images.length * (images[0].offsetWidth + 120); // 120px es el gap

    function animate() {
        position -= speed;

        // Reiniciar la posición de forma suave
        if (Math.abs(position) >= singleSetWidth) {
            position = 0;
        }

        track.style.transform = `translateX(${position}px)`;
        requestAnimationFrame(animate);
    }

    // Iniciar la animación
    animate();
});