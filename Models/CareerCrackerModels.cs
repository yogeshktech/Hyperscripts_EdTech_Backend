using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerCracker.Models
{
    public class CategoriesModels
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string categoryImage {  get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int? CouponId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; }
        public string OrderStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int CourseId { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public int Quantity { get; set; }
    }

    public class Coupon
    {
        public int Id { get; set; }
        public string CouponCode { get; set; }
        public string DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal MinOrderValue { get; set; }
        public decimal MaxDiscount { get; set; }
        public bool IsActive { get; set; }
    }

    public class RazorpaySettings
    {
        public string KeyId { get; set; }
        public string KeySecret { get; set; }
        public string Currency { get; set; }
    }

    [Table("blog_comments")]
    public class BlogComment
    {
        [Key]
        public int Id { get; set; }

        public int BlogId { get; set; }

        public string UserId { get; set; } = null!;

        public string Comment { get; set; } = null!;

        public int? ParentCommentId { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class BlogCommentResponse
    {
        public int CommentId { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserDto User { get; set; }

        public List<BlogCommentResponse> Replies { get; set; } = new();
    }

    public class UserDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }


    public class AddCommentDto
    {
        public string Comment { get; set; } = string.Empty;
        public int? ParentCommentId { get; set; } // for reply
    }

}
