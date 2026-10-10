/**
 * ===================================================================
 * WEB 3D EXPERIENCE ENGINE (Three.js + 3D Tilt + Orbit Studio)
 * DoAnNoSQL E-Commerce Platform
 * ===================================================================
 */

(function (window, document, $) {
    'use strict';

    var Web3D = {
        is3DEnabled: true,
        heroScene: null,
        heroCamera: null,
        heroRenderer: null,
        heroDeviceGroup: null,
        heroParticles: null,
        heroAutoRotate: true,
        heroAnimId: null,

        studioScene: null,
        studioCamera: null,
        studioRenderer: null,
        studioDeviceGroup: null,
        studioAutoRotate: true,
        studioAnimId: null,
        studioCurrentColor: 0x0284c7, // default Pacific Blue
        studioIsWireframe: false,

        heroCurrentModel: 'phone',
        heroRing1: null,
        heroRing2: null,

        init: function () {
            var self = this;
            $(document).ready(function () {
                self.initCardTilt();
                self.initProductImageParallax();
                self.initAmbientBackground();
                self.initCursorHalo();
                self.initHero3DShowcase();
                self.initStudioModal();
                self.initGlobalToggle();

                // Re-init tilt if dynamic products loaded (e.g. AJAX pagination/filter)
                $(document).ajaxComplete(function () {
                    self.initCardTilt();
                    self.initProductImageParallax();
                });
            });
        },

        // ===================================================================
        // 0. CYBER CURSOR HALO EFFECT
        // ===================================================================
        initCursorHalo: function () {
            if ('ontouchstart' in window || navigator.maxTouchPoints > 0) return;
            var halo = document.getElementById('cyber-cursor-halo');
            if (!halo) {
                halo = document.createElement('div');
                halo.id = 'cyber-cursor-halo';
                document.body.appendChild(halo);
            }

            var mouseX = window.innerWidth / 2;
            var mouseY = window.innerHeight / 2;
            var currentX = mouseX;
            var currentY = mouseY;

            window.addEventListener('mousemove', function (e) {
                mouseX = e.clientX;
                mouseY = e.clientY;
            });

            function renderHalo() {
                currentX += (mouseX - currentX) * 0.15;
                currentY += (mouseY - currentY) * 0.15;
                halo.style.transform = 'translate3d(' + currentX + 'px, ' + currentY + 'px, 0) translate(-50%, -50%)';
                requestAnimationFrame(renderHalo);
            }
            renderHalo();
        },

        // ===================================================================
        // 1. HARDWARE-ACCELERATED 3D CARD TILT & SPECULAR GLARE
        // ===================================================================
        initCardTilt: function () {
            if (!this.is3DEnabled) return;

            var selector = '.product-card:not([data-tilt-ready]), .product-card-3d:not([data-tilt-ready]), .cart-item:not([data-tilt-ready]), .danh-muc-item:not([data-tilt-ready]), .payment-method-option:not([data-tilt-ready])';
            var cards = document.querySelectorAll(selector);
            cards.forEach(function (card) {
                card.setAttribute('data-tilt-ready', 'true');

                // Tạo lớp phản chiếu ánh sáng Specular Glare nếu chưa có
                if (!card.querySelector('.glare-3d-layer')) {
                    var glare = document.createElement('div');
                    glare.className = 'glare-3d-layer';
                    card.appendChild(glare);
                }

                var bounds = null;

                function updateBounds() {
                    bounds = card.getBoundingClientRect();
                }

                card.addEventListener('mouseenter', updateBounds);
                window.addEventListener('resize', updateBounds);

                card.addEventListener('mousemove', function (e) {
                    if (!bounds) updateBounds();
                    var mouseX = e.clientX - bounds.left;
                    var mouseY = e.clientY - bounds.top;

                    var percentX = (mouseX / bounds.width) * 100;
                    var percentY = (mouseY / bounds.height) * 100;

                    var tiltX = ((mouseY / bounds.height) - 0.5) * -14; // góc nghiêng trục X
                    var tiltY = ((mouseX / bounds.width) - 0.5) * 14;  // góc nghiêng trục Y

                    card.style.transform = 'perspective(1000px) rotateX(' + tiltX.toFixed(2) + 'deg) rotateY(' + tiltY.toFixed(2) + 'deg) translateZ(8px)';
                    card.style.setProperty('--mouse-x', percentX.toFixed(1) + '%');
                    card.style.setProperty('--mouse-y', percentY.toFixed(1) + '%');
                });

                card.addEventListener('mouseleave', function () {
                    card.style.transform = 'perspective(1000px) rotateX(0deg) rotateY(0deg) translateZ(0px)';
                    card.style.transition = 'transform 0.5s cubic-bezier(0.25, 1, 0.5, 1)';
                    setTimeout(function () {
                        card.style.transition = '';
                    }, 500);
                });
            });
        },

        // ===================================================================
        // 1.1 PRODUCT IMAGE 3D PARALLAX TILT (ChiTietSP.cshtml)
        // ===================================================================
        initProductImageParallax: function () {
            var mainImg = document.getElementById('mainImage');
            if (!mainImg) return;

            var box = mainImg.closest('.aspect-square') || mainImg.parentElement;
            if (!box || box.hasAttribute('data-parallax-ready')) return;
            box.setAttribute('data-parallax-ready', 'true');

            box.style.perspective = '1000px';
            box.style.transformStyle = 'preserve-3d';
            mainImg.style.transition = 'transform 0.2s cubic-bezier(0.2, 0, 0.2, 1), filter 0.3s ease';
            mainImg.style.willChange = 'transform';

            var bounds = null;
            function updateBoxBounds() { bounds = box.getBoundingClientRect(); }
            box.addEventListener('mouseenter', updateBoxBounds);
            window.addEventListener('resize', updateBoxBounds);

            box.addEventListener('mousemove', function (e) {
                if (!bounds) updateBoxBounds();
                var relX = (e.clientX - bounds.left) / bounds.width - 0.5;
                var relY = (e.clientY - bounds.top) / bounds.height - 0.5;

                var rotX = relY * -20;
                var rotY = relX * 20;

                mainImg.style.transform = 'perspective(1000px) rotateX(' + rotX.toFixed(2) + 'deg) rotateY(' + rotY.toFixed(2) + 'deg) scale3d(1.08, 1.08, 1.08) translateZ(35px)';
                mainImg.style.filter = 'drop-shadow(0 20px 30px rgba(20, 184, 166, 0.25))';
            });

            box.addEventListener('mouseleave', function () {
                mainImg.style.transform = 'perspective(1000px) rotateX(0deg) rotateY(0deg) scale3d(1, 1, 1) translateZ(0px)';
                mainImg.style.filter = '';
                mainImg.style.transition = 'transform 0.5s cubic-bezier(0.25, 1, 0.5, 1), filter 0.3s ease';
                setTimeout(function () {
                    mainImg.style.transition = 'transform 0.2s cubic-bezier(0.2, 0, 0.2, 1), filter 0.3s ease';
                }, 500);
            });
        },

        // ===================================================================
        // 2. HERO 3D INTERACTIVE THREE.JS SHOWCASE
        // ===================================================================
        initHero3DShowcase: function () {
            var container = document.getElementById('hero-3d-canvas');
            if (!container || typeof THREE === 'undefined') return;

            var self = this;
            var width = container.clientWidth || 600;
            var height = container.clientHeight || 340;

            // Scene & Camera
            this.heroScene = new THREE.Scene();
            this.heroCamera = new THREE.PerspectiveCamera(45, width / height, 0.1, 1000);
            this.heroCamera.position.set(0, 0, 9);

            // Renderer with Antialias & Alpha
            this.heroRenderer = new THREE.WebGLRenderer({ alpha: true, antialias: true, powerPreference: 'high-performance' });
            this.heroRenderer.setSize(width, height);
            this.heroRenderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
            this.heroRenderer.toneMapping = THREE.ACESFilmicToneMapping;
            this.heroRenderer.toneMappingExposure = 1.2;
            container.innerHTML = '';
            container.appendChild(this.heroRenderer.domElement);

            // Lights
            var ambientLight = new THREE.AmbientLight(0xffffff, 0.7);
            this.heroScene.add(ambientLight);

            var keyLight = new THREE.DirectionalLight(0x2dd4bf, 2.5); // Teal light
            keyLight.position.set(5, 6, 7);
            this.heroScene.add(keyLight);

            var rimLight = new THREE.DirectionalLight(0x6366f1, 2.0); // Indigo rim
            rimLight.position.set(-6, -4, -4);
            this.heroScene.add(rimLight);

            var pointGlow = new THREE.PointLight(0x14b8a6, 2, 20);
            pointGlow.position.set(0, 2, 4);
            this.heroScene.add(pointGlow);

            // 3D Flagship Device Group
            this.heroDeviceGroup = new THREE.Group();
            this.heroScene.add(this.heroDeviceGroup);

            this.buildHeroDevice = function (modelType) {
                // Clear existing meshes in group
                while (self.heroDeviceGroup.children.length > 0) {
                    var obj = self.heroDeviceGroup.children[0];
                    self.heroDeviceGroup.remove(obj);
                }

                if (modelType === 'laptop') {
                    // Laptop Base
                    var baseGeom = new THREE.BoxGeometry(4.8, 0.16, 3.2);
                    var baseMat = new THREE.MeshPhysicalMaterial({ color: 0x94a3b8, metalness: 0.85, roughness: 0.25 });
                    var base = new THREE.Mesh(baseGeom, baseMat);
                    base.position.y = -0.8;
                    self.heroDeviceGroup.add(base);

                    // Trackpad
                    var padGeom = new THREE.PlaneGeometry(1.6, 1.0);
                    var padMat = new THREE.MeshStandardMaterial({ color: 0x64748b, roughness: 0.3 });
                    var pad = new THREE.Mesh(padGeom, padMat);
                    pad.rotation.x = -Math.PI / 2;
                    pad.position.set(0, -0.71, 0.8);
                    self.heroDeviceGroup.add(pad);

                    // Keyboard Area
                    var kbGeom = new THREE.PlaneGeometry(4.2, 1.6);
                    var kbMat = new THREE.MeshStandardMaterial({ color: 0x0f172a, roughness: 0.8 });
                    var kb = new THREE.Mesh(kbGeom, kbMat);
                    kb.rotation.x = -Math.PI / 2;
                    kb.position.set(0, -0.71, -0.4);
                    self.heroDeviceGroup.add(kb);

                    // Laptop Screen Lid (Angled 115 degrees)
                    var lidGroup = new THREE.Group();
                    lidGroup.position.set(0, -0.72, -1.55);

                    var lidGeom = new THREE.BoxGeometry(4.8, 3.1, 0.12);
                    var lid = new THREE.Mesh(lidGeom, baseMat);
                    lid.position.set(0, 1.55, 0);
                    lidGroup.add(lid);

                    // Screen display
                    var sGeom = new THREE.PlaneGeometry(4.6, 2.9);
                    var sCanvas = document.createElement('canvas');
                    sCanvas.width = 1024;
                    sCanvas.height = 640;
                    var sCtx = sCanvas.getContext('2d');
                    var grad = sCtx.createLinearGradient(0, 0, 1024, 640);
                    grad.addColorStop(0, '#090d16');
                    grad.addColorStop(0.5, '#042f2e');
                    grad.addColorStop(1, '#0284c7');
                    sCtx.fillStyle = grad;
                    sCtx.fillRect(0, 0, 1024, 640);
                    sCtx.fillStyle = '#2dd4bf';
                    sCtx.font = 'bold 50px sans-serif';
                    sCtx.fillText('ULTRABOOK PRO 3D', 260, 280);
                    sCtx.fillStyle = '#f8fafc';
                    sCtx.font = '32px sans-serif';
                    sCtx.fillText('M3 Max • Liquid Retina XDR • 120Hz', 220, 360);

                    var sTexture = new THREE.CanvasTexture(sCanvas);
                    var sMat = new THREE.MeshBasicMaterial({ map: sTexture });
                    var disp = new THREE.Mesh(sGeom, sMat);
                    disp.position.set(0, 1.55, 0.07);
                    lidGroup.add(disp);

                    lidGroup.rotation.x = 0.25; // mở ngửa ra sau
                    self.heroDeviceGroup.add(lidGroup);

                } else if (modelType === 'audio') {
                    // Cyber Pods Charging Case (Rounded Oval Capsule)
                    var caseGeom = new THREE.CylinderGeometry(1.8, 1.8, 2.8, 48);
                    caseGeom.rotateZ(Math.PI / 2);
                    var caseMat = new THREE.MeshPhysicalMaterial({
                        color: 0x0f172a,
                        metalness: 0.9,
                        roughness: 0.15,
                        clearcoat: 1.0,
                        clearcoatRoughness: 0.1
                    });
                    var podCase = new THREE.Mesh(caseGeom, caseMat);
                    self.heroDeviceGroup.add(podCase);

                    // LED Status Ring
                    var ledGeom = new THREE.TorusGeometry(1.82, 0.04, 16, 64);
                    ledGeom.rotateY(Math.PI / 2);
                    var ledMat = new THREE.MeshBasicMaterial({ color: 0x2dd4bf });
                    var ledRing = new THREE.Mesh(ledGeom, ledMat);
                    self.heroDeviceGroup.add(ledRing);

                    // Dual Earbuds floating above case
                    var budGeom = new THREE.SphereGeometry(0.5, 32, 32);
                    var budMat = new THREE.MeshPhysicalMaterial({ color: 0xffffff, metalness: 0.3, roughness: 0.1 });
                    var budL = new THREE.Mesh(budGeom, budMat);
                    budL.position.set(-0.9, 1.8, 0.2);
                    var budR = new THREE.Mesh(budGeom, budMat);
                    budR.position.set(0.9, 1.8, -0.2);

                    var stemGeom = new THREE.CylinderGeometry(0.12, 0.1, 1.1, 16);
                    var stemL = new THREE.Mesh(stemGeom, budMat);
                    stemL.position.set(-0.9, 1.2, 0.2);
                    var stemR = new THREE.Mesh(stemGeom, budMat);
                    stemR.position.set(0.9, 1.2, -0.2);

                    self.heroDeviceGroup.add(budL, budR, stemL, stemR);

                } else {
                    // Default: Flagship Titanium Phone
                    var phoneGeom = new THREE.BoxGeometry(2.4, 4.8, 0.22);
                    var phoneMat = new THREE.MeshPhysicalMaterial({
                        color: 0x1e293b,
                        metalness: 0.85,
                        roughness: 0.18,
                        clearcoat: 1.0,
                        clearcoatRoughness: 0.1,
                        reflectivity: 0.9
                    });
                    var phone = new THREE.Mesh(phoneGeom, phoneMat);
                    self.heroDeviceGroup.add(phone);

                    // OLED Screen
                    var screenGeom = new THREE.PlaneGeometry(2.24, 4.6);
                    var canvas = document.createElement('canvas');
                    canvas.width = 512;
                    canvas.height = 1024;
                    var ctx = canvas.getContext('2d');
                    var grad = ctx.createLinearGradient(0, 0, 512, 1024);
                    grad.addColorStop(0, '#0f172a');
                    grad.addColorStop(0.3, '#0d9488');
                    grad.addColorStop(0.7, '#3b82f6');
                    grad.addColorStop(1, '#6366f1');
                    ctx.fillStyle = grad;
                    ctx.fillRect(0, 0, 512, 1024);
                    ctx.fillStyle = '#ffffff';
                    ctx.font = 'bold 44px sans-serif';
                    ctx.fillText('NEXT-GEN 3D', 90, 360);
                    ctx.font = '28px sans-serif';
                    ctx.fillStyle = '#99f6e4';
                    ctx.fillText('TITANIUM PRO MAX', 110, 420);
                    ctx.font = '24px sans-serif';
                    ctx.fillStyle = '#f8fafc';
                    ctx.fillText('Snapdragon 8 Gen 3', 130, 480);

                    var screenTexture = new THREE.CanvasTexture(canvas);
                    var screenMat = new THREE.MeshBasicMaterial({ map: screenTexture });
                    var screen = new THREE.Mesh(screenGeom, screenMat);
                    screen.position.z = 0.12;
                    self.heroDeviceGroup.add(screen);

                    // Camera Module
                    var cameraIslandGeom = new THREE.BoxGeometry(1.0, 1.0, 0.12);
                    var cameraIslandMat = new THREE.MeshStandardMaterial({ color: 0x0f172a, metalness: 0.9, roughness: 0.2 });
                    var cameraIsland = new THREE.Mesh(cameraIslandGeom, cameraIslandMat);
                    cameraIsland.position.set(-0.55, 1.7, -0.15);
                    self.heroDeviceGroup.add(cameraIsland);

                    var lensGeom = new THREE.CylinderGeometry(0.18, 0.18, 0.08, 32);
                    lensGeom.rotateX(Math.PI / 2);
                    var lensMat = new THREE.MeshPhysicalMaterial({ color: 0x050505, roughness: 0.05, metalness: 0.95 });
                    var lens1 = new THREE.Mesh(lensGeom, lensMat);
                    lens1.position.set(-0.7, 1.9, -0.22);
                    var lens2 = new THREE.Mesh(lensGeom, lensMat);
                    lens2.position.set(-0.7, 1.5, -0.22);
                    var lens3 = new THREE.Mesh(lensGeom, lensMat);
                    lens3.position.set(-0.4, 1.7, -0.22);
                    self.heroDeviceGroup.add(lens1, lens2, lens3);
                }

                // Dual Cyber Glowing Orbit Rings
                var ringGeom1 = new THREE.TorusGeometry(3.6, 0.025, 16, 100);
                var ringMat1 = new THREE.MeshBasicMaterial({ color: 0x2dd4bf });
                self.heroRing1 = new THREE.Mesh(ringGeom1, ringMat1);
                self.heroRing1.rotation.x = Math.PI / 3;
                self.heroDeviceGroup.add(self.heroRing1);

                var ringGeom2 = new THREE.TorusGeometry(3.9, 0.02, 16, 100);
                var ringMat2 = new THREE.MeshBasicMaterial({ color: 0x6366f1 });
                self.heroRing2 = new THREE.Mesh(ringGeom2, ringMat2);
                self.heroRing2.rotation.y = Math.PI / 4;
                self.heroDeviceGroup.add(self.heroRing2);
            };

            // Build default model
            this.buildHeroDevice('phone');

            this.heroDeviceGroup.rotation.y = -0.35;
            this.heroDeviceGroup.rotation.x = 0.15;
            this.heroScene.add(this.heroDeviceGroup);

            // 5) Ambient Floating Stardust Particles
            var particleCount = 220;
            var particleGeom = new THREE.BufferGeometry();
            var positions = new Float32Array(particleCount * 3);
            for (var i = 0; i < particleCount * 3; i += 3) {
                positions[i] = (Math.random() - 0.5) * 16;
                positions[i + 1] = (Math.random() - 0.5) * 12;
                positions[i + 2] = (Math.random() - 0.5) * 12;
            }
            particleGeom.setAttribute('position', new THREE.BufferAttribute(positions, 3));
            var particleMat = new THREE.PointsMaterial({
                color: 0x5eead4,
                size: 0.08,
                transparent: true,
                opacity: 0.75
            });
            this.heroParticles = new THREE.Points(particleGeom, particleMat);
            this.heroScene.add(this.heroParticles);

            // Mouse / Touch Interaction Drag
            var isDragging = false;
            var previousMousePosition = { x: 0, y: 0 };
            var targetRotationY = -0.35;
            var targetRotationX = 0.15;

            container.addEventListener('mousedown', function (e) {
                isDragging = true;
                previousMousePosition = { x: e.clientX, y: e.clientY };
            });

            window.addEventListener('mouseup', function () {
                isDragging = false;
            });

            container.addEventListener('mousemove', function (e) {
                if (isDragging) {
                    var deltaX = e.clientX - previousMousePosition.x;
                    var deltaY = e.clientY - previousMousePosition.y;
                    targetRotationY += deltaX * 0.008;
                    targetRotationX += deltaY * 0.008;
                    previousMousePosition = { x: e.clientX, y: e.clientY };
                } else {
                    // Parallax tilt theo chuột
                    var rect = container.getBoundingClientRect();
                    var relX = (e.clientX - rect.left) / rect.width - 0.5;
                    var relY = (e.clientY - rect.top) / rect.height - 0.5;
                    targetRotationY = -0.35 + relX * 0.6;
                    targetRotationX = 0.15 + relY * 0.4;
                }
            });

            // Touch support for mobile
            container.addEventListener('touchmove', function (e) {
                if (e.touches.length === 1) {
                    var touch = e.touches[0];
                    if (isDragging) {
                        var deltaX = touch.clientX - previousMousePosition.x;
                        var deltaY = touch.clientY - previousMousePosition.y;
                        targetRotationY += deltaX * 0.01;
                        targetRotationX += deltaY * 0.01;
                        previousMousePosition = { x: touch.clientX, y: touch.clientY };
                    }
                }
            }, { passive: true });

            container.addEventListener('touchstart', function (e) {
                if (e.touches.length === 1) {
                    isDragging = true;
                    previousMousePosition = { x: e.touches[0].clientX, y: e.touches[0].clientY };
                }
            }, { passive: true });

            window.addEventListener('touchend', function () {
                isDragging = false;
            });

            // HUD Controls
            $('#hero-toggle-spin').on('click', function () {
                self.heroAutoRotate = !self.heroAutoRotate;
                $(this).toggleClass('active', self.heroAutoRotate);
            });

            $('#hero-reset-view').on('click', function () {
                targetRotationY = -0.35;
                targetRotationX = 0.15;
            });

            // Model Switcher Controls
            $('[data-hero-model]').on('click', function () {
                var modelType = $(this).data('hero-model');
                $('[data-hero-model]').removeClass('active text-teal-300 bg-teal-500/20 border-teal-400').addClass('text-slate-400 bg-white/5 border-white/10');
                $(this).addClass('active text-teal-300 bg-teal-500/20 border-teal-400').removeClass('text-slate-400 bg-white/5 border-white/10');
                self.heroCurrentModel = modelType;
                self.buildHeroDevice(modelType);
            });

            // Resize Handler
            window.addEventListener('resize', function () {
                if (!container || !self.heroCamera || !self.heroRenderer) return;
                var w = container.clientWidth || 600;
                var h = container.clientHeight || 340;
                self.heroCamera.aspect = w / h;
                self.heroCamera.updateProjectionMatrix();
                self.heroRenderer.setSize(w, h);
            });

            // Animation Loop
            var clock = new THREE.Clock();
            function animateHero() {
                self.heroAnimId = requestAnimationFrame(animateHero);

                var delta = clock.getDelta();

                if (self.heroAutoRotate && !isDragging) {
                    self.heroDeviceGroup.rotation.y += delta * 0.4;
                } else {
                    self.heroDeviceGroup.rotation.y += (targetRotationY - self.heroDeviceGroup.rotation.y) * 0.1;
                    self.heroDeviceGroup.rotation.x += (targetRotationX - self.heroDeviceGroup.rotation.x) * 0.1;
                }

                // Chuyển động quay chậm của các vòng năng lượng
                if (self.heroRing1) self.heroRing1.rotation.z += delta * 0.6;
                if (self.heroRing2) self.heroRing2.rotation.z -= delta * 0.4;

                // Hạt bụi trôi nhẹ
                if (self.heroParticles) {
                    self.heroParticles.rotation.y += delta * 0.08;
                }

                self.heroRenderer.render(self.heroScene, self.heroCamera);
            }
            animateHero();
        },

        // ===================================================================
        // 3. 3D PRODUCT ORBIT STUDIO (MODAL 360° INTERACTIVE VIEWER)
        // ===================================================================
        initStudioModal: function () {
            var self = this;

            // Chèn modal HTML vào cuối body nếu chưa tồn tại
            if (!document.getElementById('studio-3d-modal')) {
                var modalHtml = [
                    '<div id="studio-3d-modal" class="studio-3d-modal">',
                    '  <div class="studio-3d-container">',
                    '    <div class="studio-3d-header">',
                    '      <div class="flex items-center gap-3">',
                    '        <div class="w-9 h-9 rounded-xl bg-teal-500/20 border border-teal-500/40 flex items-center justify-center text-teal-400">',
                    '          <i class="fas fa-cube text-base"></i>',
                    '        </div>',
                    '        <div>',
                    '          <h3 id="studio-product-title" class="text-white font-bold text-base sm:text-lg flex items-center gap-2">',
                    '            Khám phá Không Gian 3D',
                    '            <span class="px-2 py-0.5 text-[10px] uppercase font-bold tracking-wider rounded-md bg-teal-500/30 text-teal-300 border border-teal-500/50">360° Studio</span>',
                    '          </h3>',
                    '          <p class="text-xs text-slate-400">Kéo chuột hoặc ngón tay để xoay đa chiều 360° • Lăn chuột để phóng to</p>',
                    '        </div>',
                    '      </div>',
                    '      <button id="studio-close-btn" class="w-9 h-9 rounded-full bg-slate-800/80 hover:bg-rose-500/20 text-slate-400 hover:text-rose-400 border border-slate-700 transition flex items-center justify-center">',
                    '        <i class="fas fa-times text-sm"></i>',
                    '      </button>',
                    '    </div>',
                    '    <div class="studio-3d-body">',
                    '      <div id="studio-3d-canvas-wrap" class="studio-3d-canvas-wrap"></div>',
                    '      <div class="studio-3d-toolbar">',
                    '        <button id="studio-tool-spin" class="studio-tool-btn active">',
                    '          <i class="fas fa-sync-alt"></i> <span>Tự động xoay</span>',
                    '        </button>',
                    '        <button id="studio-tool-wireframe" class="studio-tool-btn">',
                    '          <i class="fas fa-project-diagram"></i> <span>Hologram</span>',
                    '        </button>',
                    '        <button id="studio-view-front" class="studio-tool-btn">',
                    '          <i class="fas fa-mobile-alt"></i> <span>Mặt trước</span>',
                    '        </button>',
                    '        <button id="studio-view-side" class="studio-tool-btn">',
                    '          <i class="fas fa-cube"></i> <span>Góc 45°</span>',
                    '        </button>',
                    '        <button id="studio-view-back" class="studio-tool-btn">',
                    '          <i class="fas fa-camera"></i> <span>Mặt lưng</span>',
                    '        </button>',
                    '        <div class="studio-color-picker">',
                    '          <span class="text-[11px] text-slate-400 font-semibold mr-1">Vỏ máy:</span>',
                    '          <div class="color-dot-3d active" data-color="0x0284c7" style="background:#0284c7;" title="Pacific Blue"></div>',
                    '          <div class="color-dot-3d" data-color="0x1e293b" style="background:#1e293b;" title="Space Black"></div>',
                    '          <div class="color-dot-3d" data-color="0xb0bec5" style="background:#b0bec5;" title="Natural Titanium"></div>',
                    '          <div class="color-dot-3d" data-color="0xd97706" style="background:#d97706;" title="Desert Gold"></div>',
                    '          <div class="color-dot-3d" data-color="0x8b5cf6" style="background:#8b5cf6;" title="Deep Purple"></div>',
                    '        </div>',
                    '      </div>',
                    '    </div>',
                    '  </div>',
                    '</div>'
                ].join('');
                document.body.insertAdjacentHTML('beforeend', modalHtml);
            }

            // Gắn sự kiện đóng modal
            $('#studio-close-btn, #studio-3d-modal').on('click', function (e) {
                if (e.target === this || $(e.target).closest('#studio-close-btn').length) {
                    $('#studio-3d-modal').removeClass('active');
                    if (self.studioAnimId) {
                        cancelAnimationFrame(self.studioAnimId);
                    }
                }
            });

            // Nút bấm mở 3D Studio từ trang chi tiết hoặc thẻ
            $(document).on('click', '.btn-launch-3d, [data-action="open-3d-studio"]', function (e) {
                e.preventDefault();
                var prodTitle = $(this).data('title') || 'Sản phẩm Công Nghệ Cao Cấp';
                var prodImg = $(this).data('image') || '';
                self.openStudioModal(prodTitle, prodImg);
            });
        },

        openStudioModal: function (title, imgUrl) {
            var self = this;
            if (typeof THREE === 'undefined') {
                alert('Three.js đang được tải. Vui lòng thử lại sau giây lát!');
                return;
            }

            $('#studio-product-title').html(title + ' <span class="px-2 py-0.5 text-[10px] uppercase font-bold tracking-wider rounded-md bg-teal-500/30 text-teal-300 border border-teal-500/50">360° Studio</span>');
            $('#studio-3d-modal').addClass('active');

            var container = document.getElementById('studio-3d-canvas-wrap');
            var width = container.clientWidth || 800;
            var height = container.clientHeight || 560;

            // Hủy animation cũ nếu có
            if (this.studioAnimId) {
                cancelAnimationFrame(this.studioAnimId);
            }

            // Khởi tạo Scene Studio
            this.studioScene = new THREE.Scene();
            this.studioCamera = new THREE.PerspectiveCamera(40, width / height, 0.1, 1000);
            this.studioCamera.position.set(0, 0, 8.5);

            this.studioRenderer = new THREE.WebGLRenderer({ alpha: true, antialias: true, powerPreference: 'high-performance' });
            this.studioRenderer.setSize(width, height);
            this.studioRenderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
            this.studioRenderer.toneMapping = THREE.ACESFilmicToneMapping;
            this.studioRenderer.toneMappingExposure = 1.3;
            container.innerHTML = '';
            container.appendChild(this.studioRenderer.domElement);

            // Lighting Studio chuyên nghiệp
            var ambient = new THREE.AmbientLight(0xffffff, 0.8);
            this.studioScene.add(ambient);

            var keyLight = new THREE.DirectionalLight(0xffffff, 2.2);
            keyLight.position.set(6, 8, 8);
            this.studioScene.add(keyLight);

            var fillLight = new THREE.DirectionalLight(0x38bdf8, 1.4);
            fillLight.position.set(-6, -2, 5);
            this.studioScene.add(fillLight);

            var backLight = new THREE.DirectionalLight(0x14b8a6, 2.0);
            backLight.position.set(0, 6, -6);
            this.studioScene.add(backLight);

            // Tạo mô hình 3D Studio Flagship
            this.studioDeviceGroup = new THREE.Group();

            // Khung kim loại thân máy
            var bodyGeom = new THREE.BoxGeometry(2.6, 5.2, 0.24);
            var bodyMat = new THREE.MeshPhysicalMaterial({
                color: self.studioCurrentColor,
                metalness: 0.9,
                roughness: 0.18,
                clearcoat: 1.0,
                clearcoatRoughness: 0.1,
                wireframe: self.studioIsWireframe
            });
            var body = new THREE.Mesh(bodyGeom, bodyMat);
            this.studioDeviceGroup.add(body);

            // Màn hình hiển thị
            var screenGeom = new THREE.PlaneGeometry(2.44, 5.0);
            var screenMat;
            if (imgUrl) {
                var loader = new THREE.TextureLoader();
                var tex = loader.load(imgUrl, function () {
                    if (self.studioRenderer) self.studioRenderer.render(self.studioScene, self.studioCamera);
                });
                screenMat = new THREE.MeshBasicMaterial({ map: tex });
            } else {
                var sCanvas = document.createElement('canvas');
                sCanvas.width = 512;
                sCanvas.height = 1024;
                var sCtx = sCanvas.getContext('2d');
                var sGrad = sCtx.createLinearGradient(0, 0, 512, 1024);
                sGrad.addColorStop(0, '#0f172a');
                sGrad.addColorStop(0.5, '#0d9488');
                sGrad.addColorStop(1, '#1e1b4b');
                sCtx.fillStyle = sGrad;
                sCtx.fillRect(0, 0, 512, 1024);
                sCtx.fillStyle = '#ffffff';
                sCtx.font = 'bold 36px sans-serif';
                sCtx.fillText('3D ORBIT VIEW', 110, 480);
                sCtx.font = '22px sans-serif';
                sCtx.fillStyle = '#99f6e4';
                sCtx.fillText('Công nghệ Đỉnh Cao', 140, 540);
                var dynTexture = new THREE.CanvasTexture(sCanvas);
                screenMat = new THREE.MeshBasicMaterial({ map: dynTexture });
            }
            var screen = new THREE.Mesh(screenGeom, screenMat);
            screen.position.z = 0.13;
            this.studioDeviceGroup.add(screen);

            // Cụm Camera sau lưng
            var bumpGeom = new THREE.BoxGeometry(1.2, 1.2, 0.14);
            var bumpMat = new THREE.MeshStandardMaterial({ color: 0x0f172a, metalness: 0.85, roughness: 0.25 });
            var bump = new THREE.Mesh(bumpGeom, bumpMat);
            bump.position.set(-0.6, 1.8, -0.16);
            this.studioDeviceGroup.add(bump);

            var lensGeom = new THREE.CylinderGeometry(0.22, 0.22, 0.1, 32);
            lensGeom.rotateX(Math.PI / 2);
            var lensMat = new THREE.MeshPhysicalMaterial({ color: 0x050505, roughness: 0.05, metalness: 0.98 });
            var l1 = new THREE.Mesh(lensGeom, lensMat);
            l1.position.set(-0.8, 2.05, -0.24);
            var l2 = new THREE.Mesh(lensGeom, lensMat);
            l2.position.set(-0.8, 1.55, -0.24);
            var l3 = new THREE.Mesh(lensGeom, lensMat);
            l3.position.set(-0.4, 1.8, -0.24);
            this.studioDeviceGroup.add(l1, l2, l3);

            // Đĩa bệ phóng Hologram 3D bên dưới
            var pedGeom = new THREE.CylinderGeometry(2.4, 2.8, 0.1, 48);
            var pedMat = new THREE.MeshStandardMaterial({
                color: 0x0f172a,
                metalness: 0.9,
                roughness: 0.3
            });
            var ped = new THREE.Mesh(pedGeom, pedMat);
            ped.position.y = -3.2;
            this.studioScene.add(ped);

            var pedRingGeom = new THREE.TorusGeometry(2.6, 0.03, 16, 64);
            pedRingGeom.rotateX(Math.PI / 2);
            var pedRingMat = new THREE.MeshBasicMaterial({ color: 0x2dd4bf });
            var pedRing = new THREE.Mesh(pedRingGeom, pedRingMat);
            pedRing.position.y = -3.15;
            this.studioScene.add(pedRing);

            this.studioScene.add(this.studioDeviceGroup);

            // Tương tác chuột kéo xoay 360 độ (Orbit Drag)
            var isDrag = false;
            var prevMouse = { x: 0, y: 0 };
            var targetRotY = 0;
            var targetRotX = 0;

            container.onmousedown = function (e) {
                isDrag = true;
                prevMouse = { x: e.clientX, y: e.clientY };
            };

            window.onmouseup = function () {
                isDrag = false;
            };

            container.onmousemove = function (e) {
                if (!isDrag) return;
                var deltaX = e.clientX - prevMouse.x;
                var deltaY = e.clientY - prevMouse.y;
                targetRotY += deltaX * 0.01;
                targetRotX += deltaY * 0.01;
                targetRotX = Math.max(-1.2, Math.min(1.2, targetRotX));
                prevMouse = { x: e.clientX, y: e.clientY };
            };

            // Zoom in / Zoom out với cuộn chuột
            container.onwheel = function (e) {
                e.preventDefault();
                self.studioCamera.position.z += e.deltaY * 0.005;
                self.studioCamera.position.z = Math.max(4.5, Math.min(14, self.studioCamera.position.z));
            };

            // Touch events cho điện thoại
            container.ontouchstart = function (e) {
                if (e.touches.length === 1) {
                    isDrag = true;
                    prevMouse = { x: e.touches[0].clientX, y: e.touches[0].clientY };
                }
            };

            container.ontouchmove = function (e) {
                if (!isDrag || e.touches.length !== 1) return;
                var deltaX = e.touches[0].clientX - prevMouse.x;
                var deltaY = e.touches[0].clientY - prevMouse.y;
                targetRotY += deltaX * 0.012;
                targetRotX += deltaY * 0.012;
                prevMouse = { x: e.touches[0].clientX, y: e.touches[0].clientY };
            };

            window.ontouchend = function () {
                isDrag = false;
            };

            // Toolbar Actions
            $('#studio-tool-spin').off('click').on('click', function () {
                self.studioAutoRotate = !self.studioAutoRotate;
                $(this).toggleClass('active', self.studioAutoRotate);
            });

            $('#studio-tool-wireframe').off('click').on('click', function () {
                self.studioIsWireframe = !self.studioIsWireframe;
                bodyMat.wireframe = self.studioIsWireframe;
                $(this).toggleClass('active', self.studioIsWireframe);
            });

            $('#studio-view-front').off('click').on('click', function () {
                self.studioAutoRotate = false;
                $('#studio-tool-spin').removeClass('active');
                targetRotY = 0;
                targetRotX = 0;
            });

            $('#studio-view-side').off('click').on('click', function () {
                self.studioAutoRotate = false;
                $('#studio-tool-spin').removeClass('active');
                targetRotY = 0.8;
                targetRotX = 0.2;
            });

            $('#studio-view-back').off('click').on('click', function () {
                self.studioAutoRotate = false;
                $('#studio-tool-spin').removeClass('active');
                targetRotY = Math.PI;
                targetRotX = 0;
            });

            // Chọn màu vỏ máy Titanium
            $('.color-dot-3d').off('click').on('click', function () {
                $('.color-dot-3d').removeClass('active');
                $(this).addClass('active');
                var colorHex = parseInt($(this).data('color'), 16);
                self.studioCurrentColor = colorHex;
                bodyMat.color.setHex(colorHex);
            });

            // Loop Render
            var sClock = new THREE.Clock();
            function animateStudio() {
                self.studioAnimId = requestAnimationFrame(animateStudio);
                var dt = sClock.getDelta();

                if (self.studioAutoRotate && !isDrag) {
                    self.studioDeviceGroup.rotation.y += dt * 0.6;
                } else {
                    self.studioDeviceGroup.rotation.y += (targetRotY - self.studioDeviceGroup.rotation.y) * 0.12;
                    self.studioDeviceGroup.rotation.x += (targetRotX - self.studioDeviceGroup.rotation.x) * 0.12;
                }

                pedRing.rotation.z += dt * 0.8;

                self.studioRenderer.render(self.studioScene, self.studioCamera);
            }
            animateStudio();
        },

        // ===================================================================
        // 4. AMBIENT 3D STARLIGHT CANVAS
        // ===================================================================
        initAmbientBackground: function () {
            if (!this.is3DEnabled) return;
            var canvas = document.getElementById('bg-3d-particles-canvas');
            if (!canvas) {
                canvas = document.createElement('canvas');
                canvas.id = 'bg-3d-particles-canvas';
                document.body.prepend(canvas);
            }

            var ctx = canvas.getContext('2d');
            var width = (canvas.width = window.innerWidth);
            var height = (canvas.height = window.innerHeight);

            var stars = [];
            var numStars = Math.min(window.innerWidth > 768 ? 90 : 40, 100);

            for (var i = 0; i < numStars; i++) {
                stars.push({
                    x: Math.random() * width,
                    y: Math.random() * height,
                    z: Math.random() * width,
                    size: Math.random() * 1.6 + 0.4,
                    color: Math.random() > 0.5 ? '#14b8a6' : '#3b82f6',
                    alpha: Math.random() * 0.7 + 0.2
                });
            }

            window.addEventListener('resize', function () {
                width = canvas.width = window.innerWidth;
                height = canvas.height = window.innerHeight;
            });

            function renderStars() {
                ctx.clearRect(0, 0, width, height);

                for (var i = 0; i < stars.length; i++) {
                    var s = stars[i];
                    s.y -= 0.25;
                    if (s.y < 0) s.y = height;

                    ctx.beginPath();
                    ctx.arc(s.x, s.y, s.size, 0, Math.PI * 2);
                    ctx.fillStyle = s.color;
                    ctx.globalAlpha = s.alpha;
                    ctx.shadowBlur = 8;
                    ctx.shadowColor = s.color;
                    ctx.fill();
                }

                requestAnimationFrame(renderStars);
            }
            renderStars();
        },

        // ===================================================================
        // 5. GLOBAL 3D MODE TOGGLE SWITCH HUD
        // ===================================================================
        initGlobalToggle: function () {
            var self = this;
            if (!$('.switch-3d-toggle').length) {
                var toggleBtn = $([
                    '<div class="switch-3d-toggle" title="Bật/Tắt hiệu ứng Web 3D">',
                    '  <span class="toggle-indicator"></span>',
                    '  <span>3D MODE ON</span>',
                    '</div>'
                ].join(''));

                $('body').append(toggleBtn);

                toggleBtn.on('click', function () {
                    self.is3DEnabled = !self.is3DEnabled;
                    if (self.is3DEnabled) {
                        $(this).find('span:last').text('3D MODE ON');
                        $(this).find('.toggle-indicator').css('background', '#10b981');
                        $('#bg-3d-particles-canvas').show();
                        self.initCardTilt();
                    } else {
                        $(this).find('span:last').text('3D MODE OFF');
                        $(this).find('.toggle-indicator').css('background', '#ef4444');
                        $('#bg-3d-particles-canvas').hide();
                        $('.product-card, .product-card-3d').css('transform', '');
                    }
                });
            }
        }
    };

    window.Web3DExperience = Web3D;
    Web3D.init();

})(window, document, jQuery);
