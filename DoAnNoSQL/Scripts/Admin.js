
    // Biến toàn cục (để demo)
    let rowToDelete = null;
    let confirmDeleteModal = null;
    // Biến đếm ID để thêm mới (demo)
    let newProductId = 2;
    let newCategoryId = 2;
    let newBrandId = 2;
    let newCustomerId = 2;
    let newPromoId = 2;
    let newAdminId = 2;

    // --- HÀM CƠ BẢN ---
    function showTab(tabId) {
        // Hiển thị tab được chọn
        document.querySelectorAll('.tab-content').forEach(tab => tab.classList.remove('active'));
    document.getElementById(tabId).classList.add('active');

    // Cập nhật URL hash
    window.location.hash = tabId;

    if (tabId === 'reports') {
        renderCharts();
            }
        }

    function showToast(type, message) {
            const toastEl = document.getElementById('liveToast');
    const toastBody = toastEl.querySelector('.toast-body');

    // Đảm bảo xóa các class bg cũ
    toastEl.classList.remove('bg-success', 'bg-danger', 'bg-info', 'bg-warning', 'text-white');

    let headerClass = '';
    if (type === 'success') {
        headerClass = 'bg-success';
            } else if (type === 'danger') {
        headerClass = 'bg-danger';
            } else if (type === 'info') {
        headerClass = 'bg-info';
            } else {
        headerClass = 'bg-secondary';
            }

    toastEl.classList.add(headerClass, 'text-white');
    toastBody.textContent = message;

    const bsToast = new bootstrap.Toast(toastEl, {delay: 3000 });
    bsToast.show();
        }

    function formatCurrency(num) {
            // Chuyển đổi chuỗi thành số nếu cần
            const number = typeof num === 'string' ? parseFloat(num.replace(/[^0-9]/g, '')) : num;
    if (isNaN(number)) {
                return "0 VNĐ"; // Trả về giá trị mặc định nếu không phải là số
            }
    return new Intl.NumberFormat('vi-VN', {style: 'currency', currency: 'VND' }).format(number);
        }

    // --- HÀM KHỞI TẠO VÀ SỰ KIỆN ---
    document.addEventListener('DOMContentLoaded', function () {
            // KHÔI PHỤC: Khởi tạo modal xác nhận xóa
            if (document.getElementById('confirmDeleteModal')) {
        confirmDeleteModal = new bootstrap.Modal(document.getElementById('confirmDeleteModal'));
            }

    //--- KÍCH HOẠT CHỨC NĂNG LỌC VÀ TÌM KIẾM ---
    setupEventListeners();

    //--- KÍCH HOẠT IMPORT/EXPORT ---
    setupImportExport();

    // KHÔI PHỤC: Kiểm tra hash trên URL khi tải trang
    const hash = window.location.hash.substring(1); // Lấy hash (bỏ dấu #)
    if (hash) {
                const targetTab = document.getElementById(hash);
    if (targetTab && targetTab.classList.contains('tab-content')) {
        showTab(hash);
                }
            }
        });

    function setupEventListeners() {
        const addListener = (id, event, callback) => {
            const el = document.getElementById(id);
            if (el) el.addEventListener(event, callback);
        };

        // Gắn sự kiện cho tất cả các ô tìm kiếm
        addListener('searchProduct', 'input', () => filterTable('productTableBody', 'searchProduct'));
        addListener('searchCategory', 'input', () => filterTable('categoryTableBody', 'searchCategory'));
        addListener('searchBrand', 'input', () => filterTable('brandTableBody', 'searchBrand'));
        addListener('searchCustomer', 'input', () => filterTable('customerTableBody', 'searchCustomer'));
        addListener('searchOrder', 'input', () => filterTable('orderTableBody', 'searchOrder', ['filterOrder']));
        addListener('searchPayment', 'input', () => filterTable('paymentTableBody', 'searchPayment'));
        addListener('searchPromotion', 'input', () => filterTable('promotionTableBody', 'searchPromotion'));
        addListener('searchAdmin', 'input', () => filterTable('adminTableBody', 'searchAdmin'));
        addListener('searchSupport', 'input', () => filterTable('supportTableBody', 'searchSupport', ['filterSupport']));

        // Gắn sự kiện cho các bộ lọc
        addListener('filterOrder', 'change', () => filterTable('orderTableBody', 'searchOrder', ['filterOrder']));
        addListener('filterSupport', 'change', () => filterTable('supportTableBody', 'searchSupport', ['filterSupport']));
    }

    // --- ĐÃ XÓA: CÁC HÀM CRUD (setupCrudListeners, handleTableClick, confirmDelete, prepareAddModal, prepareEditModal, saveProduct, v.v.) ---


    function setupImportExport() {
        // 1. Lắng nghe sự kiện Import CSV
        document.getElementById('csvImportInput').addEventListener('change', importCSV);
            // 2. Lắng nghe sự kiện Export CSV
            document.getElementById('exportCsvBtn').addEventListener('click', () => exportTableToCSV('productTable', 'danh-sach-san-pham.csv'));
            // 3. Lắng nghe sự kiện Export Excel Báo cáo
            document.getElementById('exportReportExcelBtn').addEventListener('click', () => exportTableToExcel('reportDetailTable', 'BaoCaoDoanhThu.xlsx'));
        }

    // --- HÀM LỌC BẢNG ---
    function filterTable(tableBodyId, searchInputId, filterSelectIds = []) {
            const searchTerm = document.getElementById(searchInputId).value.toLowerCase();
    const tableBody = document.getElementById(tableBodyId);
    const rows = tableBody.querySelectorAll('tr');

    const filterValues = { };
            filterSelectIds.forEach(id => {
        filterValues[id] = document.getElementById(id).value;
            });

            rows.forEach(row => {
        let isVisible = true;
    const rowText = row.textContent.toLowerCase();

    // 1. Lọc theo từ khóa tìm kiếm
    if (!rowText.includes(searchTerm)) {
        isVisible = false;
                }

                // 2. Lọc theo các <select>
                if (isVisible && filterSelectIds.length > 0) {
                    if (filterSelectIds.includes('filterOrder')) {
                        const status = row.cells[4].textContent.trim();
        if (filterValues['filterOrder'] !== 'Tất cả' && status !== filterValues['filterOrder']) {
            isVisible = false;
                        }
                    }
        if (filterSelectIds.includes('filterSupport')) {
                        const status = row.cells[3].textContent.trim();
        if (filterValues['filterSupport'] !== 'Tất cả' && status !== filterValues['filterSupport']) {
            isVisible = false;
                        }
                    }
                }

        row.style.display = isVisible ? '' : 'none';
            });
        }


        // --- HÀM VẼ BIỂU ĐỒ ---
        function renderCharts() {
            // Biểu đồ doanh thu
            const revenueCtx = document.getElementById('revenueChart').getContext('2d');
        if (window.myRevenueChart) {
            window.myRevenueChart.destroy();
            }
        window.myRevenueChart = new Chart(revenueCtx, {
            type: 'line',
        data: {
            labels: ['Tháng 1', 'Tháng 2', 'Tháng 3', 'Tháng 4', 'Tháng 5', 'Tháng 6'],
        datasets: [{
            label: 'Doanh thu (triệu VNĐ)',
        data: [50, 70, 90, 65, 80, 100],
        borderColor: '#007bff',
        backgroundColor: 'rgba(0, 123, 255, 0.2)',
        fill: true
                    }]
                },
        options: {responsive: true, scales: {y: {beginAtZero: true } } }
            });

        // Biểu đồ sản phẩm bán chạy
        const topProductsCtx = document.getElementById('topProductsChart').getContext('2d');
        if (window.myTopProductsChart) {
            window.myTopProductsChart.destroy();
            }
        window.myTopProductsChart = new Chart(topProductsCtx, {
            type: 'bar',
        data: {
            labels: ['iPhone 14', 'Samsung S23', 'Dell XPS'],
        datasets: [{
            label: 'Số lượng bán',
        data: [150, 120, 80],
        backgroundColor: ['#007bff', '#28a745', '#ffc107']
                    }]
                },
        options: {responsive: true, scales: {y: {beginAtZero: true } } }
            });
        }


        // --- CÁC HÀM IMPORT, EXPORT, VÀ IN ---

        /**
         * Hàm Import CSV sử dụng PapaParse
         */
        function importCSV(event) {
            const file = event.target.files[0];
        if (!file) {
            showToast('danger', 'Bạn chưa chọn file.');
        return;
            }

        Papa.parse(file, {
            header: true,
        skipEmptyLines: true,
        complete: function (results) {
                    const tableBody = document.getElementById('productTableBody');

                    results.data.forEach(row => {
                        const newId = newProductId++;
        const newRow = document.createElement('tr');
        newRow.id = `product-${newId}`;

        // Lấy dữ liệu từ CSV (đảm bảo an toàn)
        const tenSanPham = row.TenSanPham || 'N/A';
        const danhMuc = row.DanhMuc || 'N/A';
        const thuongHieu = row.ThuongHieu || 'N/A';
        const gia = row.Gia || 0;
        const tonKho = row.TonKho || 0;
        const moTaNgan = row.MoTaNgan || '';
        const moTaChiTiet = row.MoTaChiTiet || '';
        const boNho = row.BoNhoTrong || '';
        const ram = row.Ram || '';
        const mangHinh = row.MangHinh || '';
        const pin = row.Pin || '';
        const camera = row.Camera || '';
        const hdh = row.HeDieuHanh || '';
        const congKetNoi = row.CongKetNoi || '';


        newRow.innerHTML = `
        <td>${newId}</td>
        <td><img data-src="https://placehold.co/50x50/aaa/white?text=CSV" alt="${tenSanPham}" class="lazyload"></td>
        <td>${tenSanPham}</td>
        <td>${danhMuc}</td>
        <td>${thuongHieu}</td>
        <td>${formatCurrency(gia)}</td>
        <td>${tonKho}</td>
        <td><span class="badge bg-info">Imported</span></td>
        <td>${moTaNgan}</td>
        <td>${moTaChiTiet}</td>
        <td>${boNho}</td>
        <td>${ram}</td>
        <td>${mangHinh}</td>
        <td>${pin}</td>
        <td>${camera}</td>
        <td>${hdh}</td>
        <td>${congKetNoi}</td>
        <td>
            <a href="/products/edit/${newId}" class="btn btn-sm btn-warning"><i class="fas fa-edit"></i> Sửa</a>
            <a href="/products/delete/${newId}" class="btn btn-sm btn-danger" onclick="return confirm('Bạn có chắc chắn muốn xóa?');"><i class="fas fa-trash"></i> Xóa</a>
        </td>
        `;
        tableBody.appendChild(newRow);
                    });
        showToast('success', `Đã import thành công ${results.data.length} sản phẩm!`);
        event.target.value = null; // Reset input
        lazySizes.init(); // Kích hoạt lazyload
                },
        error: function (err) {
            showToast('danger', 'Lỗi khi đọc file: ' + err.message);
                }
            });
        }

        /**
         * Hàm Export dữ liệu từ bảng HTML ra CSV
         */
        function exportTableToCSV(tableId, filename) {
            let csv = [];
        const table = document.getElementById(tableId);
        const rows = table.querySelectorAll("tr");

        for (let i = 0; i < rows.length; i++) {
            let row = [], cols = rows[i].querySelectorAll("td, th");

        // Bỏ qua cột cuối (Hành động)
        for (let j = 0; j < cols.length - 1; j++) {
            let data = cols[j].innerText.replace(/,/g, "").replace(/\n/g, " "); // Xóa dấu phẩy, xuống dòng
        row.push('"' + data + '"');
                }
        csv.push(row.join(","));
            }

        downloadCSV(csv.join("\n"), filename);
        showToast('success', 'Đã xuất file CSV thành công!');
        }

        function downloadCSV(csvContent, filename) {
            const bom = "\uFEFF"; // BOM cho UTF-8 (để Excel đọc tiếng Việt)
        const blob = new Blob([bom + csvContent], {type: 'text/csv;charset=utf-8;' });
        const link = document.createElement("a");

        if (link.download !== undefined) {
                const url = URL.createObjectURL(blob);
        link.setAttribute("href", url);
        link.setAttribute("download", filename);
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
            }
        }

        /**
         * Hàm Export Excel cho tab Báo cáo (dùng SheetJS)
         */
        function exportTableToExcel(tableId, filename) {
            const table = document.getElementById(tableId);
        if (!table) {
            showToast('danger', 'Không tìm thấy bảng báo cáo.');
        return;
            }
        const wb = XLSX.utils.table_to_book(table, {sheet: "BaoCaoChiTiet" });
        XLSX.writeFile(wb, filename);
        showToast('success', 'Đã xuất file Excel thành công!');
        }

        /**
         * ĐÃ KHÔI PHỤC: Hàm hiển thị Modal In Hóa Đơn
         */
        function showInvoiceModal(btn) {
            const row = btn.closest('tr');
        if (!row) return;

        const orderId = row.cells[0].textContent;
        const customerName = row.cells[1].textContent;
        const orderTotal = row.cells[2].textContent;
        const orderDate = row.cells[3].textContent;

        document.getElementById('invoiceOrderId').textContent = orderId;
        document.getElementById('invoiceCustomerName').textContent = customerName;
        document.getElementById('invoiceCustomerEmail').textContent = customerName.replace(/\s/g, '.').toLowerCase() + '@example.com';
        document.getElementById('invoiceOrderDate').textContent = orderDate;
        document.getElementById('invoiceTotal').textContent = orderTotal;

        // Demo: Thêm chi tiết sản phẩm (bạn sẽ cần logic phức tạp hơn ở đây)
        document.getElementById('invoiceItems').innerHTML = `
        <tr>
            <td>(Sản phẩm chi tiết của đơn #${orderId})</td>
            <td>1</td>
            <td>${orderTotal}</td>
            <td>${orderTotal}</td>
        </tr>
        `;

        const invoiceModal = new bootstrap.Modal(document.getElementById('invoiceModal'));
        invoiceModal.show();
        }

        /**
         * ĐÃ KHÔI PHỤC: Hàm In nội dung của một khu vực cụ thể
         */
        function printModalContent(elementId) {
            const printableArea = document.getElementById(elementId);
            
            // Cập nhật ngày in
            const printDateTimeEl = document.getElementById('printDateTime');
            if (printDateTimeEl) {
                const now = new Date();
                const formattedDate = now.toLocaleDateString('vi-VN') + ' ' + now.toLocaleTimeString('vi-VN');
                printDateTimeEl.textContent = 'Ngày in: ' + formattedDate;
            }

            // Create temporary print container
            const printContainer = document.createElement('div');
            printContainer.id = 'print-container';
            printContainer.innerHTML = printableArea.innerHTML;
            
            // Remove buttons in print container
            const buttons = printContainer.querySelectorAll('.btn, .btn-close, .modal-footer');
            buttons.forEach(btn => btn.remove());
            
            document.body.appendChild(printContainer);
            document.body.classList.add('printing-modal');

            window.print();

            // Cleanup
            setTimeout(() => {
                document.body.classList.remove('printing-modal');
                const pc = document.getElementById('print-container');
                if (pc) {
                    pc.remove();
                }
            }, 500);
        }

        /**
         * Hàm In Báo cáo (Vẫn giữ lại)
         */
        function printReport() {
            const reportTab = document.getElementById('reports');
        reportTab.classList.add('printable-area');

        window.print();

        window.onafterprint = function () {
            reportTab.classList.remove('printable-area');
        window.onafterprint = null;
            };

            // Fallback nếu onafterprint không được hỗ trợ
            setTimeout(() => {
                if (reportTab.classList.contains('printable-area')) {
            reportTab.classList.remove('printable-area');
                }
            }, 1000);
        }
