// brand-carousel.js

document.addEventListener('DOMContentLoaded', function () {
    const track = document.getElementById('brandCarouselTrack');

    if (!track) return;

    // Clonar las im�genes m�ltiples veces para un efecto m�s suave
    const images = track.querySelectorAll('.brand-carousel-img');
    const numClones = 3; // N�mero de veces que se repiten los logos

    // Crear m�ltiples copias de los logos
    for (let i = 0; i < numClones; i++) {
        images.forEach(img => {
            const clone = img.cloneNode(true);
            track.appendChild(clone);
        });
    }

    // Velocidad del carrusel
    let position = 0;
    const speed = 0.5; // p�xeles por frame

    // Calcular el ancho de un set completo de im�genes
    const singleSetWidth = images.length * (images[0].offsetWidth + 120); // 120px es el gap

    function animate() {
        position -= speed;

        // Reiniciar la posici�n de forma suave
        if (Math.abs(position) >= singleSetWidth) {
            position = 0;
        }

        track.style.transform = `translateX(${position}px)`;
        requestAnimationFrame(animate);
    }

    // Iniciar la animaci�n
    animate();
});