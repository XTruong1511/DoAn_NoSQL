document.addEventListener('DOMContentLoaded', () => {
    // Xử lý toggle sidebar
    const sidebar = document.getElementById('sidebar');
    const sidebarOverlay = document.getElementById('sidebarOverlay');
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebarToggleWrapper = document.getElementById('sidebarToggleWrapper');

    sidebarToggle.addEventListener('click', () => {
        sidebar.classList.toggle('active');
        sidebarOverlay.classList.toggle('active');
        
        // Hide/show toggle button
        if (sidebar.classList.contains('active')) {
            sidebarToggleWrapper.style.opacity = '0';
            sidebarToggleWrapper.style.pointerEvents = 'none';
        } else {
            sidebarToggleWrapper.style.opacity = '1';
            sidebarToggleWrapper.style.pointerEvents = 'auto';
        }
    });

    sidebarOverlay.addEventListener('click', () => {
        sidebar.classList.remove('active');
        sidebarOverlay.classList.remove('active');
        
        // Show toggle button
        sidebarToggleWrapper.style.opacity = '1';
        sidebarToggleWrapper.style.pointerEvents = 'auto';
        
        closeAllCategories();
    });

    /* --- SLIDER BANNER TỰ ĐỘNG + CHẤM DOT ---*/
    const slides = document.getElementById("bannerSlides");
    const dots = document.querySelectorAll("[data-index]");
    let index = 0;
    const total = dots.length;

    function showSlide(i) {
        index = (i + total) % total;
        slides.style.transform = `translateX(-${index * 100}%)`;
        dots.forEach((dot, idx) => {
            dot.classList.toggle("bg-blue-600", idx === index);
            dot.classList.toggle("bg-gray-300", idx !== index);
        });
    }

    dots.forEach(dot => {
        dot.addEventListener("click", () => {
            index = parseInt(dot.dataset.index);
            showSlide(index);
        });
    });

    setInterval(() => {
        index = (index + 1) % total;
        showSlide(index);
    }, 4000);


    // --- PRODUCT SLIDER SCRIPT ---
    function initProductSlider(sliderElement) {
        const slides = sliderElement.querySelector('.product-slides');
        const cards = sliderElement.querySelectorAll('.product-card');
        const prevBtn = sliderElement.querySelector('.prev-btn');
        const nextBtn = sliderElement.querySelector('.next-btn');

        if (cards.length === 0) return;

        let currentIndex = 0;

        function updateSliderPosition() {
            const cardWidth = cards[0].offsetWidth;
            const slidesStyle = window.getComputedStyle(slides);
            const gap = parseFloat(slidesStyle.gap) || 0;
            const totalSlideDistance = cardWidth + gap;
            slides.style.transform = `translateX(-${currentIndex * totalSlideDistance}px)`;
            updateButtons();
        }

        function updateButtons() {
            prevBtn.style.display = currentIndex === 0 ? 'none' : 'flex';
            const containerWidth = sliderElement.offsetWidth;
            const totalSlidesWidth = slides.scrollWidth;
            const hiddenWidth = totalSlidesWidth - containerWidth;
            const currentPosition = currentIndex * (cards[0].offsetWidth + (parseFloat(window.getComputedStyle(slides).gap) || 0));
            nextBtn.style.display = currentPosition >= hiddenWidth - 1 ? 'none' : 'flex';
        }

        nextBtn.addEventListener('click', () => {
            currentIndex++;
            updateSliderPosition();
        });

        prevBtn.addEventListener('click', () => {
            if (currentIndex > 0) {
                currentIndex--;
                updateSliderPosition();
            }
        });

        // Use ResizeObserver for better performance than 'resize' event
        new ResizeObserver(updateSliderPosition).observe(sliderElement);

        // Initial call
        updateSliderPosition();
    }

    document.querySelectorAll('.product-slider').forEach(initProductSlider);

    // --- XỬ LÝ TOGGLE SIDEBAR ---
    // Xử lý toggle danh sách category
    document.querySelectorAll('.toggle-category').forEach(item => {
        item.addEventListener('click', (e) => {
            e.preventDefault();
            const categoryList = item.nextElementSibling;
            if (categoryList && categoryList.classList.contains('category-list')) {
                const isActive = categoryList.classList.contains('active');
                closeAllCategories();
                if (!isActive) {
                    categoryList.classList.add('active');
                }
            }
        });
    });

    function closeAllCategories() {
        document.querySelectorAll('.category-list').forEach(list => {
            list.classList.remove('active');
        });
    }

    // --- MỞ / ĐÓNG MENU TRÊN MOBILE ---
    function toggleMobileMenu() {
        const menu = document.getElementById('mobile-menu');
        menu.classList.toggle('hidden');
    }

    // --- CHUYỂN TAB NỘI DUNG ---
    function openTab(evt, tabName) {
        const tabContents = document.getElementsByClassName('tab-content');
        for (let i = 0; i < tabContents.length; i++) {
            tabContents[i].classList.remove('active');
        }
        const tabLinks = document.getElementsByClassName('tab-link');
        for (let i = 0; i < tabLinks.length; i++) {
            tabLinks[i].classList.remove('text-blue-600');
            tabLinks[i].classList.add('text-gray-500');
        }
        document.getElementById(tabName).classList.add('active');
        evt.currentTarget.classList.remove('text-gray-500');
        evt.currentTarget.classList.add('text-blue-600');
    }

    // --- CHỌN TAB ĐẦU TIÊN KHI TRANG MỚI TẢI ---
    document.addEventListener('DOMContentLoaded', function () {
        const firstTab = document.querySelector('.tab-content');
        const firstTabLink = document.querySelector('.tab-link');
        if (firstTab) firstTab.classList.add('active');
        if (firstTabLink) {
            firstTabLink.classList.remove('text-gray-500');
            firstTabLink.classList.add('text-blue-600');
        }
    });

   

    // --- THÊM SẢN PHẨM VÀO GIỎ HÀNG ---
    document.querySelectorAll('.btn-add-to-cart').forEach(button => {
        button.addEventListener('click', function () {
            const productCard = this.closest('.product-card');
            const productName = productCard.querySelector('h3').textContent;
            
            // Use SweetAlert2 for better UX
            Swal.fire({
                icon: 'success',
                title: 'Thành công!',
                text: `Sản phẩm "${productName}" đã được thêm vào giỏ hàng`,
                confirmButtonColor: '#28a745',
                timer: 2000,
                timerProgressBar: true,
                showConfirmButton: false
            });
        });
    });
});