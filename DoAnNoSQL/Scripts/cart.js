/**
 * Shopping Cart AJAX Functions
 * X? lý các ch?c n?ng gi? hàng v?i AJAX
 */

// Add to cart from product detail page
function addToCartAjax(productId, quantity, color) {
    quantity = quantity || 1;
    color = color || '';

    // Get selected color from radio buttons
    if (!color) {
        var selectedColor = $('input[name="color"]:checked').val();
      color = selectedColor || '';
    }

    $.ajax({
        url: '/GioHang/AddToCart',
        type: 'POST',
        data: {
   productId: productId,
      quantity: quantity,
       color: color
   },
        beforeSend: function() {
 // Show loading state
        $('.btn-add-to-cart').prop('disabled', true).html('<i class="fas fa-spinner fa-spin mr-2"></i>?ang thêm...');
        },
        success: function(response) {
  if (response.success) {
 // Show success notification
       showCartNotification(response.message, 'success');
    
 // Update cart count in header
   updateCartCount(response.cartCount);
              
       // Optional: Show mini cart
    // showMiniCart();
  } else {
  showCartNotification(response.message, 'error');
            }
      },
        error: function() {
  showCartNotification('Có l?i x?y ra, vui lòng th? l?i', 'error');
        },
        complete: function() {
            // Reset button state
   $('.btn-add-to-cart').prop('disabled', false).html('<i class="fas fa-shopping-cart mr-2"></i>Thêm vào gi? hàng');
        }
    });
}

// Update cart count in header
function updateCartCount(count) {
    $('.cart-count').text(count);

    // Animate cart icon
    $('.cart-icon').addClass('animate-bounce');
    setTimeout(function() {
        $('.cart-icon').removeClass('animate-bounce');
    }, 1000);
}

// Show cart notification
function showCartNotification(message, type) {
var bgColor = type === 'success' ? 'bg-green-500' : 'bg-red-500';
 var icon = type === 'success' ? 'check-circle' : 'exclamation-circle';
    
    var notification = $('<div>')
        .addClass('fixed top-20 right-4 ' + bgColor + ' text-white px-6 py-4 rounded-lg shadow-2xl z-50')
        .css('animation', 'slideInRight 0.3s ease-out')
 .html('<div class="flex items-center">' +
        '<i class="fas fa-' + icon + ' text-2xl mr-3"></i>' +
              '<div>' +
          '<p class="font-bold">' + (type === 'success' ? 'Thành công!' : 'L?i!') + '</p>' +
  '<p class="text-sm">' + message + '</p>' +
         '</div>' +
    '</div>');
    
    $('body').append(notification);
    
    setTimeout(function() {
 notification.fadeOut(300, function() {
     $(this).remove();
        });
}, 3000);
}

// Get cart count (for header display)
function getCartCount() {
    $.ajax({
        url: '/GioHang/GetCartCount',
        type: 'GET',
        success: function(response) {
      updateCartCount(response.count);
        }
    });
}

// Quick add to cart from product list
$(document).on('click', '.quick-add-to-cart', function(e) {
    e.preventDefault();
    var productId = $(this).data('product-id');
    addToCartAjax(productId, 1, '');
});

// Initialize on page load
$(document).ready(function() {
  // Load cart count
    getCartCount();
    
    // Add to cart from product detail page
    $('#add-to-cart-form').on('submit', function(e) {
        e.preventDefault();
  var productId = $(this).find('input[name="sanPhamId"]').val();
        var quantity = $(this).find('#quantity').val();
        var color = $(this).find('input[name="color"]:checked').val() || '';
        
        addToCartAjax(productId, quantity, color);
 });
});

// CSS Animation
var style = document.createElement('style');
style.textContent = `
    @@keyframes slideInRight {
 from {
         transform: translateX(100%);
    opacity: 0;
        }
     to {
            transform: translateX(0);
       opacity: 1;
        }
    }
    
    .animate-bounce {
        animation: bounce 1s;
    }
    
    @@keyframes bounce {
        0%, 100% {
            transform: translateY(0);
        }
        50% {
     transform: translateY(-10px);
   }
    }
`;
document.head.appendChild(style);
