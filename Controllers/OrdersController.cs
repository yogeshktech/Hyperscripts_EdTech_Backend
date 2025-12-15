using CareerCracker.BusinessLayer;
using CareerCracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Razorpay;
using Razorpay.Api;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace CareerCracker.Controllers
{
    [EnableCors("CorsPolicy")]
    [ApiController]
    [Route("api/order")]
    [Authorize(Roles = "USER,ADMIN,SUPERADMIN")]
    public class OrdersController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        private readonly RazorpaySettings _rz;

        public OrdersController(IBusinessLayer businessLayer, IOptions<RazorpaySettings> razorpayOptions)
        {
            _businessLayer = businessLayer;
            _rz = razorpayOptions.Value;
        }

        [Authorize]
        [Route("checkout")]
        [HttpGet]
        public async Task<IActionResult> CheckOut()
        {
            try
            {
                string userEmail =
                    User.FindFirst(ClaimTypes.Email)?.Value ??
                    User.FindFirst("email")?.Value ??
                    User.FindFirst("UserEmail")?.Value;

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized();

                return await _businessLayer.CheckOut(userEmail);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        [Route("create-order")]
        [HttpPost]
        public async Task<IActionResult> CreateOrder(IFormCollection form)
        {
            try
            {
                string userEmail =
                User.FindFirst(ClaimTypes.Email)?.Value ??
                User.FindFirst("email")?.Value ??
                User.FindFirst("UserEmail")?.Value;
                return await _businessLayer.CreateOrder(userEmail,form);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        [Route("add-order-item")]
        [HttpPost]
        public async Task<IActionResult> AddOrderItem(IFormCollection form)
        {
            try
            {
                return await _businessLayer.AddOrderItem(form);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("razorpay/create")]
        public IActionResult CreateRazorpay(IFormCollection form)
        {
            decimal amount = decimal.Parse(form["amount"]);
            int orderId = int.Parse(form["order_id"]);

            var client = new RazorpayClient(_rz.KeyId, _rz.KeySecret);

            var options = new Dictionary<string, object>
    {
        { "amount", amount * 100 }, // in paise
        { "currency", _rz.Currency },
        { "receipt", $"order_{orderId}" },
        { "payment_capture", 1 }
    };

            Razorpay.Api.Order razorpayOrder = client.Order.Create(options);

            return Ok(new
            {
                success = true,
                razorpay_order_id = razorpayOrder["id"].ToString(),
                key = _rz.KeyId
            });
        }


        [HttpPost("razorpay/verify")]
        public async Task<IActionResult> Verify(IFormCollection form)
        {
            string razorpayOrderId = form["razorpay_order_id"];
            string razorpayPaymentId = form["razorpay_payment_id"];
            string razorpaySignature = form["razorpay_signature"];
            int orderId = int.Parse(form["order_id"]);

            string payload = razorpayOrderId + "|" + razorpayPaymentId;

            using var hmac = new HMACSHA256(
                Encoding.UTF8.GetBytes(_rz.KeySecret)
            );

            string generatedSignature = BitConverter
                .ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)))
                .Replace("-", "")
                .ToLower();

            if (generatedSignature != razorpaySignature)
                return BadRequest(new { success = false, message = "Payment verification failed" });

            return await _businessLayer.MarkPaymentPaid(orderId);
        }



        [Route("get-order")]
        [HttpGet]
        public async Task<IActionResult> GetOrder(int orderId)
        {
            try
            {
                return await _businessLayer.GetOrder(orderId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        [Route("all")]
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                return await _businessLayer.GetAllOrders();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

    }

}
