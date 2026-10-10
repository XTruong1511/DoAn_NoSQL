/* ==========================================================================
   1. Các Hàm Toàn Cục (Có thể gọi từ thuộc tính onclick trong HTML)
   ========================================================================== */

/**
 * [Trang chủ] Bật/tắt menu điều hướng trên di động.
 */
function toggleMobileMenu() {
    const menu = document.getElementById('mobile-menu');
    if (menu) {
        menu.classList.toggle('hidden'); // Thêm/xóa class 'hidden' để ẩn/hiện menu
    }
}

/**
 * [Trang chủ] Chuyển đổi qua lại giữa các tab nội dung.
 * @param {Event} evt - Sự kiện click.
 * @param {string} tabName - ID của nội dung tab cần hiển thị.
 */
function openTab(evt, tabName) {
    // Ẩn tất cả nội dung của các tab
    const tabContents = document.getElementsByClassName('tab-content');
    for (let i = 0; i < tabContents.length; i++) {
        tabContents[i].classList.remove('active');
    }

    // Bỏ trạng thái active của tất cả các nút tab
    const tabLinks = document.getElementsByClassName('tab-link');
    for (let i = 0; i < tabLinks.length; i++) {
        tabLinks[i].classList.remove('text-blue-600');
        tabLinks[i].classList.add('text-gray-500');
    }

    // Hiển thị nội dung và đánh dấu active cho tab được chọn
    const activeTab = document.getElementById(tabName);
    if (activeTab) {
        activeTab.classList.add('active');
        evt.currentTarget.classList.remove('text-gray-500');
        evt.currentTarget.classList.add('text-blue-600');
    }
}

/**
 * [Chi tiết sản phẩm] Thay đổi ảnh chính của sản phẩm khi nhấn vào ảnh thumbnail.
 * @param {string} imageSrc - Đường dẫn của ảnh lớn.
 * @param {HTMLElement} element - Thẻ ảnh thumbnail đã được nhấn.
 */
function changeImage(imageSrc, element) {
    const mainImage = document.getElementById('mainProductImage');
    if (mainImage) {
        mainImage.style.transform = 'scale(1)'; // Reset hiệu ứng zoom khi đổi ảnh
        mainImage.src = imageSrc;
    }

    // Xóa class 'active' khỏi tất cả thumbnail
    let thumbnails = document.querySelectorAll('.thumbnail-images img');
    thumbnails.forEach(thumb => {
        thumb.classList.remove('active');
    });
    // Thêm class 'active' cho thumbnail vừa được nhấn
    element.classList.add('active');
}

/**
 * [Chi tiết sản phẩm] Thay đổi giá trị số lượng trong ô nhập liệu.
 * @param {number} amount - Lượng thay đổi (+1 để tăng, -1 để giảm).
 */
function changeQuantity(amount) {
    let quantityInput = document.getElementById('quantityInput');
    if (quantityInput) {
        let currentValue = parseInt(quantityInput.value);
        let newValue = currentValue + amount;
        // Đảm bảo số lượng không nhỏ hơn 1
        if (newValue >= 1) {
            quantityInput.value = newValue;
        }
    }
}


/* ==========================================================================
   2. DOMContentLoaded - Chạy các đoạn mã theo từng trang sau khi trang tải xong
   ========================================================================== */

document.addEventListener('DOMContentLoaded', function () {

    // --- Logic cho Trang Chủ ---
    const sidebar = document.getElementById('sidebar');
    if (sidebar) { // Chỉ chạy code này nếu tìm thấy element 'sidebar' (đúng là trang chủ)
        const sidebarOverlay = document.getElementById('sidebarOverlay');
        const sidebarToggle = document.getElementById('sidebarToggle');

        // Bật/tắt sidebar
        if (sidebarToggle) {
            sidebarToggle.addEventListener('click', () => {
                sidebar.classList.toggle('active');
                sidebarOverlay.classList.toggle('active');
            });
        }

        // Đóng sidebar khi nhấn vào lớp phủ mờ
        if (sidebarOverlay) {
            sidebarOverlay.addEventListener('click', () => {
                sidebar.classList.remove('active');
                sidebarOverlay.classList.remove('active');
                closeAllCategories();
            });
        }

        // Mở/đóng danh mục con (accordion menu)
        document.querySelectorAll('.toggle-category').forEach(item => {
            item.addEventListener('click', (e) => {
                e.preventDefault();
                const categoryList = item.nextElementSibling;
                if (categoryList && categoryList.classList.contains('category-list')) {
                    const isActive = categoryList.classList.contains('active');
                    closeAllCategories(); // Đóng tất cả các mục khác
                    if (!isActive) {
                        categoryList.classList.add('active'); // Mở mục hiện tại
                    }
                }
            });
        });

        function closeAllCategories() {
            document.querySelectorAll('.category-list').forEach(list => {
                list.classList.remove('active');
            });
        }

        // Kích hoạt tab đầu tiên khi tải trang
        const firstTab = document.querySelector('.tab-content');
        const firstTabLink = document.querySelector('.tab-link');
        if (firstTab) firstTab.classList.add('active');
        if (firstTabLink) {
            firstTabLink.classList.remove('text-gray-500');
            firstTabLink.classList.add('text-blue-600');
        }

        // --- BANNER SLIDER SCRIPT (Gộp từ script thứ 2) ---
        const bannerSlidesContainer = document.getElementById('banner-slides-container');
        const bannerDotsContainer = document.getElementById('banner-dots');
        if (bannerSlidesContainer && bannerDotsContainer) {
            const slides = bannerSlidesContainer.querySelectorAll('.banner-slide');
            const slideCount = slides.length;
            let currentSlide = 0;
            let autoPlayInterval;

            // Create dots
            if (slideCount > 0) {
                for (let i = 0; i < slideCount; i++) {
                    const dot = document.createElement('button');
                    dot.classList.add('w-3', 'h-3', 'rounded-full', 'transition');
                    dot.dataset.slide = i;
                    bannerDotsContainer.appendChild(dot);
                }
                const dots = bannerDotsContainer.querySelectorAll('button');

                function goToSlide(slideIndex) {
                    currentSlide = slideIndex;
                    bannerSlidesContainer.style.transform = `translateX(-${currentSlide * 100}%)`;
                    updateDots();
                }

                function updateDots() {
                    dots.forEach((dot, index) => {
                        if (index === currentSlide) {
                            dot.classList.add('bg-white');
                            dot.classList.remove('bg-gray-400');
                        } else {
                            dot.classList.add('bg-gray-400');
                            dot.classList.remove('bg-white');
                        }
                    });
                }

                function startAutoPlay() {
                    autoPlayInterval = setInterval(() => {
                        currentSlide = (currentSlide + 1) % slideCount;
                        goToSlide(currentSlide);
                    }, 5000); // Change slide every 5 seconds
                }

                function stopAutoPlay() {
                    clearInterval(autoPlayInterval);
                }

                dots.forEach(dot => {
                    dot.addEventListener('click', () => {
                        stopAutoPlay();
                        goToSlide(parseInt(dot.dataset.slide));
                        startAutoPlay();
                    });
                });

                // Initial setup
                goToSlide(0);
                startAutoPlay();
            }
        }

        // --- PRODUCT SLIDER SCRIPT (Gộp từ script thứ 2) ---
        function initProductSlider(sliderElement) {
            const slides = sliderElement.querySelector('.product-slides');
            const cards = sliderElement.querySelectorAll('.product-card');
            const prevBtn = sliderElement.querySelector('.prev-btn');
            const nextBtn = sliderElement.querySelector('.next-btn');

            if (!slides || cards.length === 0 || !prevBtn || !nextBtn) return;

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
                const totalSlidesWidth = (cards.length * cards[0].offsetWidth) + ((cards.length - 1) * (parseFloat(window.getComputedStyle(slides).gap) || 0));

                // Khoảng cách tối đa có thể trượt
                const maxTranslateX = totalSlidesWidth - containerWidth;

                // Vị trí hiện tại của slider
                const currentPosition = currentIndex * (cards[0].offsetWidth + (parseFloat(window.getComputedStyle(slides).gap) || 0));

                nextBtn.style.display = currentPosition >= maxTranslateX - 1 ? 'none' : 'flex';
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

        // Thông báo khi thêm vào giỏ hàng
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
    }

    // --- Logic cho Trang Chi Tiết Sản Phẩm ---
    const mainProductImageContainer = document.querySelector('.main-image-container');
    if (mainProductImageContainer) { // Chỉ chạy nếu ở trang chi tiết sản phẩm
        const img = document.getElementById('mainProductImage');

        // Hiệu ứng zoom ảnh khi di chuột
        mainProductImageContainer.addEventListener('mousemove', (e) => {
            const rect = mainProductImageContainer.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;
            const xPercent = (x / rect.width) * 100;
            const yPercent = (y / rect.height) * 100;
            img.style.transformOrigin = `${xPercent}% ${yPercent}%`;
            img.style.transform = 'scale(1.75)';
        });

        // Reset ảnh khi rời chuột
        mainProductImageContainer.addEventListener('mouseleave', () => {
            img.style.transform = 'scale(1)';
            img.style.transformOrigin = 'center center';
        });

        // Xử lý khi chọn các tùy chọn (ví dụ: dung lượng)
        document.querySelectorAll('.option-btn-group').forEach(group => {
            let buttons = group.querySelectorAll('.btn');
            buttons.forEach(button => {
                button.addEventListener('click', function () {
                    buttons.forEach(btn => btn.classList.remove('active')); // Bỏ active ở các nút khác
                    this.classList.add('active'); // Thêm active cho nút được nhấn
                });
            });
        });

        // Xử lý khi chọn màu sắc
        let colorDots = document.querySelectorAll('.color-options .color-dot');
        colorDots.forEach(dot => {
            dot.addEventListener('click', function () {
                colorDots.forEach(d => d.classList.remove('active'));
                this.classList.add('active');
            });
        });

        // Xử lý nút Yêu thích
        const wishlistBtn = document.getElementById('wishlistBtn');
        if (wishlistBtn) {
            wishlistBtn.addEventListener('click', function () {
                const icon = this.querySelector('i');
                icon.classList.toggle('far'); // Chuyển đổi giữa trái tim rỗng
                icon.classList.toggle('fas'); // và trái tim đặc
                icon.classList.toggle('text-danger'); // Thêm/bỏ màu đỏ
            });
        }

        // Đánh giá sao tương tác trong form review
        const stars = document.querySelectorAll('.review-form-rating .fa-star');
        const ratingValue = document.getElementById('ratingValue');
        if (stars.length > 0 && ratingValue) {
            function resetStars() {
                stars.forEach(star => {
                    if (!star.classList.contains('rated')) {
                        star.classList.replace('fas', 'far');
                    }
                });
            }

            stars.forEach(star => {
                // Hiệu ứng khi di chuột qua
                star.addEventListener('mouseover', function () {
                    resetStars();
                    const rating = this.dataset.rating;
                    for (let i = 0; i < rating; i++) {
                        stars[i].classList.replace('far', 'fas');
                    }
                });
                // Trả về trạng thái đã chọn khi rời chuột
                star.addEventListener('mouseout', function () {
                    resetStars();
                    const currentRating = ratingValue.value;
                    if (currentRating > 0) {
                        for (let i = 0; i < currentRating; i++) {
                            stars[i].classList.replace('far', 'fas');
                            stars[i].classList.add('rated');
                        }
                    }
                });
                // Lưu giá trị khi nhấn chọn
                star.addEventListener('click', function () {
                    const rating = this.dataset.rating;
                    ratingValue.value = rating;
                    stars.forEach(s => s.classList.remove('rated'));
                    for (let i = 0; i < rating; i++) {
                        stars[i].classList.add('rated');
                    }
                });
            });
        }

        // Đồng hồ đếm ngược khuyến mãi
        const countdownElement = document.getElementById('countdown');
        if (countdownElement) {
            const countdownDate = new Date().setDate(new Date().getDate() + 3); // Đặt thời gian kết thúc là 3 ngày sau
            let timerInterval = setInterval(function () {
                const now = new Date().getTime();
                const distance = countdownDate - now;

                const days = Math.floor(distance / (1000 * 60 * 60 * 24));
                const hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
                const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
                const seconds = Math.floor((distance % (1000 * 60)) / 1000);

                document.getElementById('days').innerText = days;
                document.getElementById('hours').innerText = hours;
                document.getElementById('minutes').innerText = minutes;
                document.getElementById('seconds').innerText = seconds;

                if (distance < 0) {
                    clearInterval(timerInterval);
                    countdownElement.innerHTML = "KHUYẾN MÃI ĐÃ KẾT THÚC";
                }
            }, 1000);
        }

        // Thông báo "Toast" khi thêm vào giỏ hàng
        const addToCartBtn = document.getElementById('addToCartBtn');
        const cartToast = document.getElementById('cartToast');
        if (addToCartBtn && cartToast) {
            addToCartBtn.addEventListener('click', function () {
                cartToast.classList.add('show');
                // Tự động ẩn sau 3 giây
                setTimeout(function () {
                    cartToast.classList.remove('show');
                }, 3000);
            });
        }
    }

    // --- Logic cho Trang Khuyến Mãi ---
    const mainPromoBanner = document.querySelector('.main-promo-banner');
    if (mainPromoBanner) { // Chỉ chạy nếu ở trang khuyến mãi
        // Đồng hồ đếm ngược sự kiện
        const countdown = () => {
            const countDate = new Date("October 30, 2025 00:00:00").getTime(); // Đặt ngày kết thúc sự kiện
            const now = new Date().getTime();
            const gap = countDate - now;

            if (gap < 0) {
                document.getElementById('countdown-timer').innerHTML = "<h4 class='w-100'>Sự kiện đã kết thúc!</h4>";
                clearInterval(countdownInterval);
                return;
            }

            const second = 1000, minute = second * 60, hour = minute * 60, day = hour * 24;
            const textDay = Math.floor(gap / day);
            const textHour = Math.floor((gap % day) / hour);
            const textMinute = Math.floor((gap % hour) / minute);
            const textSecond = Math.floor((gap % minute) / second);
            const format = (num) => num < 10 ? '0' + num : num;

            document.getElementById('days').innerText = format(textDay);
            document.getElementById('hours').innerText = format(textHour);
            document.getElementById('minutes').innerText = format(textMinute);
            document.getElementById('seconds').innerText = format(textSecond);
        };
        const countdownInterval = setInterval(countdown, 1000);

        // Lọc các khuyến mãi theo danh mục
        const filterLinks = document.querySelectorAll('.filter-nav .nav-link');
        const promoItems = document.querySelectorAll('.promo-item');
        filterLinks.forEach(link => {
            link.addEventListener('click', function (e) {
                e.preventDefault();
                filterLinks.forEach(l => l.classList.remove('active'));
                this.classList.add('active');
                const filter = this.getAttribute('data-filter');
                promoItems.forEach(item => {
                    const categoryMatch = filter === 'all' || item.getAttribute('data-category') === filter;
                    if (!item.classList.contains('hidden')) { // Chỉ áp dụng hiệu ứng cho item đang hiển thị
                        if (categoryMatch) {
                            item.style.opacity = '0';
                            setTimeout(() => {
                                item.style.display = 'block';
                                setTimeout(() => item.style.opacity = '1', 50);
                            }, 10);
                        } else {
                            item.style.opacity = '0';
                            setTimeout(() => item.style.display = 'none', 400);
                        }
                    }
                });
            });
        });

        // Nút sao chép mã khuyến mãi
        document.querySelectorAll('.copy-code-btn').forEach(button => {
            button.addEventListener('click', () => {
                const targetId = button.dataset.clipboardTarget;
                const textToCopy = document.querySelector(targetId).innerText;
                navigator.clipboard.writeText(textToCopy).then(() => {
                    button.classList.add('copied');
                    setTimeout(() => button.classList.remove('copied'), 2000); // Bỏ hiệu ứng sau 2s
                });
            });
        });

        // Nút "Xem thêm"
        const loadMoreBtn = document.getElementById('load-more-btn');
        if (loadMoreBtn) {
            const loadMoreContainer = document.getElementById('load-more-container');
            loadMoreBtn.addEventListener('click', () => {
                const hiddenItems = document.querySelectorAll('.promo-item.hidden');
                const itemsToShow = Array.from(hiddenItems).slice(0, 3); // Hiện 3 mục mỗi lần nhấn
                if (itemsToShow.length > 0) {
                    itemsToShow.forEach(item => {
                        item.classList.remove('hidden');
                        item.style.opacity = '0';
                        setTimeout(() => item.style.opacity = '1', 50); // Hiệu ứng fade-in
                    });
                }
                // Ẩn nút "Xem thêm" nếu đã hết mục để hiển thị
                if (document.querySelectorAll('.promo-item.hidden').length === 0) {
                    loadMoreContainer.style.display = 'none';
                }
            });
        }
    }

    // --- Logic cho Trang Đăng Nhập / Đăng Ký ---
    const authContainer = document.querySelector('.auth-container');
    if (authContainer) {
        const loginForm = document.getElementById('login-form');
        const registerForm = document.getElementById('register-form');
        const showRegisterLink = document.getElementById('show-register-form');
        const showLoginLink = document.getElementById('show-login-form');

        if (showRegisterLink) {
            // Chuyển sang form đăng ký
            showRegisterLink.addEventListener('click', (e) => {
                e.preventDefault();
                loginForm.classList.remove('active');
                registerForm.classList.add('active');
            });
        }

        if (showLoginLink) {
            // Chuyển lại form đăng nhập
            showLoginLink.addEventListener('click', (e) => {
                e.preventDefault();
                registerForm.classList.remove('active');
                loginForm.classList.add('active');
            });
        }

        // Chức năng hiện/ẩn mật khẩu
        document.querySelectorAll('.password-toggle').forEach(toggle => {
            toggle.addEventListener('click', function () {
                const targetId = this.getAttribute('data-target');
                const passwordInput = document.getElementById(targetId);
                if (passwordInput.type === 'password') {
                    passwordInput.type = 'text';
                    this.classList.remove('fa-eye');
                    this.classList.add('fa-eye-slash');
                } else {
                    passwordInput.type = 'password';
                    this.classList.remove('fa-eye-slash');
                    this.classList.add('fa-eye');
                }
            });
        });
    }

    // --- Logic cho Trang Tin Tức / Blog (Hoặc các trang dài khác) ---
    const scrollTopBtn = document.getElementById("scrollTopBtn");
    if (scrollTopBtn) {
        // Hiển thị nút khi cuộn trang xuống 200px
        window.onscroll = function () {
            if (document.body.scrollTop > 200 || document.documentElement.scrollTop > 200) {
                scrollTopBtn.style.display = "block";
            } else {
                scrollTopBtn.style.display = "none";
            }
        };
        // Cuộn lên đầu trang khi nhấn nút
        scrollTopBtn.addEventListener("click", function () {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });
    }
});

/* ==========================================================================
   3. Các Module Tự Chứa (IIFE - Immediately Invoked Function Expressions)
   ========================================================================== */

// --- Logic cho Trang Liên Hệ ---
(() => {
    'use strict'
    const form = document.getElementById('contact-form');
    if (!form) return; // Thoát nếu không phải trang liên hệ

    const submitButton = document.getElementById('submit-button');
    const spinner = submitButton.querySelector('.spinner-border');
    const buttonText = submitButton.querySelector('.button-text');
    const formFeedback = document.getElementById('form-feedback');

    form.addEventListener('submit', event => {
        event.preventDefault(); // Ngăn form gửi đi theo cách mặc định
        event.stopPropagation();

        if (form.checkValidity()) {
            // Nếu form hợp lệ, xử lý gửi (giả lập)
            spinner.style.display = 'inline-block';
            buttonText.textContent = 'Đang gửi...';
            submitButton.disabled = true;
            formFeedback.innerHTML = ''; // Xóa thông báo cũ

            // Giả lập độ trễ mạng
            setTimeout(() => {
                // Reset lại nút bấm
                spinner.style.display = 'none';
                buttonText.textContent = 'Gửi Lời Nhắn';
                submitButton.disabled = false;

                // Hiển thị thông báo thành công
                formFeedback.innerHTML = `
                    <div class="alert alert-success" role="alert">
                        Cảm ơn bạn! Lời nhắn đã được gửi thành công. Chúng tôi sẽ phản hồi sớm nhất có thể.
                    </div>
                `;

                // Xóa dữ liệu và trạng thái validation của form
                form.reset();
                form.classList.remove('was-validated');

            }, 1500); // 1.5 giây

        } else {
            // Nếu form không hợp lệ, hiển thị thông báo lỗi của Bootstrap
            form.classList.add('was-validated');
        }
    }, false);
})();


// --- Logic cho Trang Thanh Toán ---
(() => {
    'use strict'
    const form = document.getElementById('checkout-form');
    if (!form) return; // Thoát nếu không phải trang thanh toán

    const submitButton = document.getElementById('submit-button');
    const spinner = submitButton.querySelector('.spinner-border');
    const buttonText = submitButton.querySelector('.button-text');
    const modalEl = document.getElementById('orderSuccessModal');
    const orderSuccessModal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);

    // Xử lý validation form của Bootstrap
    form.addEventListener('submit', event => {
        event.preventDefault(); // Ngăn form gửi đi ngay
        if (!form.checkValidity()) {
            event.stopPropagation();
        } else {
            // Nếu form hợp lệ, hiển thị spinner
            spinner.style.display = 'inline-block';
            buttonText.textContent = 'Đang xử lý...';
            submitButton.disabled = true;

            // Giả lập quá trình xử lý đơn hàng
            setTimeout(() => {
                spinner.style.display = 'none';
                buttonText.textContent = 'Hoàn tất đặt hàng';
                submitButton.disabled = false;

                // Hiển thị modal thông báo thành công
                document.getElementById('order-id').textContent = `TECH${Math.floor(Math.random() * 90000) + 10000}`;
                orderSuccessModal.show();
            }, 2000); // 2 giây
        }
        form.classList.add('was-validated');
    }, false);

    // --- XỬ LÝ ĐỊA CHỈ ĐỘNG 3 CẤP (API) ---
    const provinceSelect = document.getElementById('province');
    const districtSelect = document.getElementById('district');
    const wardSelect = document.getElementById('ward');
    const API_HOST = "https://provinces.open-api.vn/api/";

    // Hàm gọi API
    async function callApi(endpoint) {
        try {
            const response = await fetch(API_HOST + endpoint);
            if (!response.ok) throw new Error('Network response was not ok');
            return response.json();
        } catch (error) {
            console.error('Failed to fetch API:', error);
        }
    }

    // Hàm hiển thị các lựa chọn (tỉnh, huyện, xã)
    function renderOptions(selectElement, data, placeholder) {
        selectElement.innerHTML = `<option value="" disabled selected>${placeholder}</option>`;
        if (Array.isArray(data)) {
            data.forEach(item => {
                const option = document.createElement('option');
                option.value = item.code;
                option.textContent = item.name;
                selectElement.appendChild(option);
            });
        }
    }

    // Tải danh sách tỉnh/thành khi trang được tải
    async function loadProvinces() {
        const provinces = await callApi('p/');
        renderOptions(provinceSelect, provinces, 'Chọn Tỉnh/Thành');
    }

    // Khi chọn tỉnh/thành -> Tải quận/huyện
    provinceSelect.addEventListener('change', async () => {
        const provinceCode = provinceSelect.value;
        districtSelect.innerHTML = '<option value="" disabled selected>Đang tải...</option>';
        wardSelect.innerHTML = '<option value="" disabled selected>Chọn Phường/Xã</option>';
        districtSelect.disabled = false;
        wardSelect.disabled = true;
        const data = await callApi(`p/${provinceCode}?depth=2`);
        renderOptions(districtSelect, data.districts, 'Chọn Quận/Huyện');
    });

    // Khi chọn quận/huyện -> Tải phường/xã
    districtSelect.addEventListener('change', async () => {
        const districtCode = districtSelect.value;
        wardSelect.innerHTML = '<option value="" disabled selected>Đang tải...</option>';
        wardSelect.disabled = false;
        const data = await callApi(`d/${districtCode}?depth=2`);
        renderOptions(wardSelect, data.wards, 'Chọn Phường/Xã');
    });


    // --- XỬ LÝ PHƯƠNG THỨC THANH TOÁN ---
    const paymentOptionsContainer = document.getElementById('payment-options');
    const paymentMethods = document.querySelectorAll('input[name="paymentMethod"]');
    const creditCardInfo = document.getElementById('creditCardInfo');
    const bankTransferInfo = document.getElementById('bankTransferInfo');

    function updatePaymentSelection() {
        // Bỏ chọn tất cả
        if (paymentOptionsContainer) {
            paymentOptionsContainer.querySelectorAll('.payment-option').forEach(opt => opt.classList.remove('selected'));
        }
        const selectedRadio = document.querySelector('input[name="paymentMethod"]:checked');
        if (selectedRadio) {
            // Thêm class 'selected' cho thẻ cha để tạo hiệu ứng viền
            selectedRadio.closest('.payment-option').classList.add('selected');
            // Hiển thị/ẩn các form thông tin thẻ/ngân hàng tương ứng
            if (creditCardInfo) creditCardInfo.style.display = (selectedRadio.id === 'credit') ? 'block' : 'none';
            if (bankTransferInfo) bankTransferInfo.style.display = (selectedRadio.id === 'bankTransfer') ? 'block' : 'none';
        }
    }
    paymentMethods.forEach(method => method.addEventListener('change', updatePaymentSelection));
    updatePaymentSelection(); // Chạy lần đầu khi tải trang


    // --- TÍNH TOÁN VÀ CẬP NHẬT GIÁ TRỊ ĐƠN HÀNG ---
    const shippingMethods = document.querySelectorAll('input[name="shippingMethod"]');
    let baseSubtotal = 0;
    let currentShippingCost = 0;
    let currentDiscount = 0;
    const formatCurrency = (value) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value);

    // Hàm cập nhật tất cả các giá trị
    function updateTotals() {
        const subtotalEl = document.getElementById('subtotal-price');
        const shippingFeeEl = document.getElementById('shipping-fee');
        const totalPriceEl = document.getElementById('total-price');
        const cartItemCountEl = document.getElementById('cart-item-count');
        const discountAmountEl = document.getElementById('discount-amount');
        const discountRow = document.getElementById('discount-row');

        // Tính lại tạm tính từ các sản phẩm còn lại
        baseSubtotal = Array.from(document.querySelectorAll('.product-item')).reduce((sum, item) => sum + parseFloat(item.dataset.price), 0);
        // Cập nhật phí ship
        const selectedShipping = document.querySelector('input[name="shippingMethod"]:checked');
        if (selectedShipping) {
            currentShippingCost = selectedShipping.id === 'shippingExpress' ? 30000 : 0;
        } else {
            currentShippingCost = 0;
        }

        // Hiển thị giảm giá nếu có
        if (currentDiscount > 0) {
            if (discountAmountEl) discountAmountEl.textContent = `- ${formatCurrency(currentDiscount)}`;
            if (discountRow) discountRow.style.display = 'flex';
        } else {
            if (discountRow) discountRow.style.display = 'none';
        }
        // Tính tổng cuối cùng
        const finalTotal = baseSubtotal - currentDiscount + currentShippingCost;
        // Cập nhật giao diện
        if (subtotalEl) subtotalEl.textContent = formatCurrency(baseSubtotal);
        if (shippingFeeEl) shippingFeeEl.textContent = currentShippingCost > 0 ? formatCurrency(currentShippingCost) : 'Miễn phí';
        if (totalPriceEl) totalPriceEl.textContent = formatCurrency(finalTotal > 0 ? finalTotal : 0);
        if (cartItemCountEl) cartItemCountEl.textContent = document.querySelectorAll('.product-item').length;
    }

    // Lắng nghe sự kiện thay đổi phương thức vận chuyển
    shippingMethods.forEach(method => method.addEventListener('change', updateTotals));

    // Xử lý mã giảm giá
    const couponForm = document.getElementById('coupon-form');
    if (couponForm) {
        const couponInput = document.getElementById('coupon-input');
        const couponFeedback = document.getElementById('coupon-feedback');
        couponForm.addEventListener('submit', function (e) {
            e.preventDefault();
            const code = couponInput.value.trim().toUpperCase();
            couponFeedback.textContent = '';
            // Giả lập mã giảm giá hợp lệ
            if (code === 'GIAM10') {
                currentDiscount = baseSubtotal * 0.1; // Giảm 10%
                couponFeedback.classList.add('text-success');
                couponFeedback.classList.remove('text-danger');
                couponFeedback.textContent = 'Áp dụng mã giảm giá thành công!';
            } else {
                currentDiscount = 0; // Xóa giảm giá nếu mã sai
                couponFeedback.classList.add('text-danger');
                couponFeedback.classList.remove('text-success');
                couponFeedback.textContent = 'Mã giảm giá không hợp lệ.';
            }
            updateTotals(); // Cập nhật lại tổng tiền
        });
    }

    // Xử lý xóa sản phẩm khỏi đơn hàng
    const cartItemsContainer = document.getElementById('cart-items');
    if (cartItemsContainer) {
        cartItemsContainer.addEventListener('click', function (e) {
            if (e.target.classList.contains('remove-item-btn')) {
                e.target.closest('.product-item').remove();
                updateTotals(); // Cập nhật lại tổng tiền sau khi xóa
            }
        });
    }

    // Xử lý thu gọn/mở rộng tóm tắt đơn hàng trên mobile
    const mobileToggle = document.getElementById('mobileSummaryToggle');
    if (mobileToggle) {
        const summaryContent = document.getElementById('orderSummaryContent');
        const toggleIcon = mobileToggle.querySelector('.fa-chevron-down');
        mobileToggle.addEventListener('click', () => {
            const isHidden = summaryContent.style.display === 'none' || summaryContent.style.display === '';
            summaryContent.style.display = isHidden ? 'block' : 'none';
            toggleIcon.classList.toggle('fa-chevron-down');
            toggleIcon.classList.toggle('fa-chevron-up');
        });
    }

    // Khởi chạy các hàm cần thiết khi tải trang
    loadProvinces(); // Tải danh sách tỉnh thành
    updateTotals(); // Cập nhật giá trị đơn hàng

})();