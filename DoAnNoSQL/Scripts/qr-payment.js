$(document).ready(function() {
    // Xử lý khi click vào phương thức thanh toán
    $('input[name="PaymentMethod"]').on('change', function() {
        var paymentMethod = $(this).val();
        var $submitBtn = $('#btnSubmitOrder');
        
        // Hiển thị thông báo cho phương thức chuyển khoản
        if (paymentMethod == '2' || paymentMethod == '3' || paymentMethod == '4') {
            var methodName = getPaymentMethodName(paymentMethod);
            $submitBtn.attr('data-payment-method', paymentMethod);
            $submitBtn.find('.payment-text').html('<i class="fas fa-credit-card"></i> Thanh toán qua ' + methodName);
        } else {
            $submitBtn.removeAttr('data-payment-method');
            $submitBtn.find('.payment-text').html('<i class="fas fa-check-circle"></i> Xác nhận đặt hàng');
        }
    });
});

/**
 * Lấy tên phương thức thanh toán
 */
function getPaymentMethodName(paymentMethod) {
    switch(paymentMethod) {
        case '2': return 'VNPay';
        case '3': return 'MoMo';
        case '4': return 'ZaloPay';
        default: return '';
    }
}
