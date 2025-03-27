//using System.Security.Claims;
//using ManagerApp.Clients;
//using ManagerApp.Controllers.Orders;
//using ManagerApp.DTO.Orders;
//using ManagerApp.Models.Orders;
//using Microsoft.AspNetCore.Mvc;
//using Moq;
//using Xunit;
//using Xunit.Abstractions;

//namespace ManagerApp.Tests
//{
//    public class OrdersControllerTests
//    {
//        private readonly Mock<IOrderServiceClient> _mockOrderServiceClient;
//        private readonly Mock<ILogger<OrdersController>> _mockLogger;
//        private readonly OrdersController _controller;
//        private readonly ITestOutputHelper _testOutputHelper;

//        public OrdersControllerTests(ITestOutputHelper testOutputHelper)
//        {
//            _mockOrderServiceClient = new Mock<IOrderServiceClient>();
//            _mockLogger = new Mock<ILogger<OrdersController>>();
//            _controller = new OrdersController(_mockOrderServiceClient.Object, _mockLogger.Object);
//            _testOutputHelper = testOutputHelper;

//            var httpContext = new DefaultHttpContext();
//            var cookieCollection = new Mock<IRequestCookieCollection>();
//            cookieCollection.Setup(c => c["accessToken"]).Returns("test-token");
//            httpContext.Request.Cookies = cookieCollection.Object;
//            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
//        }

//        /// <summary>
//        /// ✅ Проверка успешного получения списка заказов.
//        /// </summary>
//        [Fact]
//        public async Task GetOrders_ReturnsJsonResult_WhenOrdersExist()
//        {
//            // Arrange
//            var mockOrders = new List<OrderResponse> { new() { Id = Guid.NewGuid() } };
//            _mockOrderServiceClient
//                .Setup(client => client.GetAllOrdersAsync(It.IsAny<string>()))
//                .ReturnsAsync(mockOrders);

//            _testOutputHelper.WriteLine("🔄 Тест: Получение списка заказов...");

//            // Act
//            var result = await _controller.GetOrders();

//            _testOutputHelper.WriteLine("✅ Результат теста: {0}", result);

//            // Assert
//            var jsonResult = Assert.IsType<JsonResult>(result);
//            Assert.Equal(mockOrders, jsonResult.Value);
//        }

//        /// <summary>
//        /// ❌ Проверка ошибки, если отсутствует accessToken.
//        /// </summary>
//        [Fact]
//        public async Task GetOrders_ReturnsUnauthorized_WhenNoAccessToken()
//        {
//            // Arrange
//            var httpContext = _controller.ControllerContext.HttpContext;
//            var cookieCollection = new Mock<IRequestCookieCollection>();
//            cookieCollection.Setup(c => c["accessToken"]).Returns((string?)null);
//            httpContext.Request.Cookies = cookieCollection.Object;

//            _testOutputHelper.WriteLine("🔄 Тест: Проверка запроса без accessToken...");

//            // Act
//            var result = await _controller.GetOrders();

//            _testOutputHelper.WriteLine("✅ Результат теста: {0}", result);

//            // Assert
//            Assert.IsType<UnauthorizedObjectResult>(result);
//        }

//        /// <summary>
//        /// ✅ Проверка успешного создания заявки.
//        /// </summary>
//        [Fact]
//        public async Task CreateOrder_ReturnsJsonResult_WhenSuccessful()
//        {
//            // Arrange
//            var managerId = Guid.NewGuid();
//            var request = new OrderRequest
//            {
//                OrderType = OrderType.Installation,
//                InstallationDate = DateTimeOffset.UtcNow.AddDays(2),
//                InstallationAddress = "123 Main Street",
//                PaymentMethod = PaymentMethod.Cash,
//                Equipment = new OrderRequest.EquipmentDTO
//                {
//                    ModelName = "Test AC",
//                    ModelSource = "Warehouse",
//                    BTU = 12000,
//                    ServiceArea = 35,
//                    Price = 500,
//                    Quantity = 1
//                },
//                FullName = "Test Client",
//                PhoneNumber = "+123456789",
//                Email = "test@example.com",
//                FulfillmentStatus = FulfillmentStatus.New,
//                PaymentStatus = PaymentStatus.Unpaid,
//                WorkCost = 100,
//                TechnicianIds = new List<Guid> { Guid.NewGuid() }
//            };

//            var createdOrder = new CreatedOrderResponseDTO { OrderId = Guid.NewGuid() };

//            _mockOrderServiceClient
//                .Setup(client => client.CreateOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<string>()))
//                .ReturnsAsync(createdOrder);

//            // Мокируем accessToken
//            var cookieCollection = new Mock<IRequestCookieCollection>();
//            cookieCollection.Setup(c => c["accessToken"]).Returns("test-token");

//            // Мокируем пользователя (имитируем залогиненного менеджера)
//            var user = new ClaimsPrincipal(new ClaimsIdentity(
//            new[]
//            {
//                new Claim(ClaimTypes.NameIdentifier, managerId.ToString())
//            }, "mock"));

//            var httpContext = new DefaultHttpContext();
//            httpContext.Request.Cookies = cookieCollection.Object;
//            httpContext.User = user;
//            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

//            _testOutputHelper.WriteLine("🔄 Тест: Создание новой заявки...");
//            _testOutputHelper.WriteLine("🔍 accessToken: {0}", httpContext.Request.Cookies["accessToken"]);
//            _testOutputHelper.WriteLine("🔍 ManagerId: {0}", managerId);

//            // Act
//            var result = await _controller.CreateOrder(request);

//            _testOutputHelper.WriteLine("✅ Результат теста: {0}", result);

//            // Assert
//            Assert.NotNull(result);
//            var jsonResult = Assert.IsType<JsonResult>(result);
//            Assert.NotNull(jsonResult.Value);
//            Assert.Equal(createdOrder, jsonResult.Value);
//        }

//        /// <summary>
//        /// ❌ Проверка ошибки при создании заявки, если сервис возвращает null.
//        /// </summary>
//        [Fact]
//        public async Task CreateOrder_ReturnsServerError_WhenServiceFails()
//        {
//            // Arrange
//            var managerId = Guid.NewGuid();
//            var request = new OrderRequest
//            {
//                OrderType = OrderType.Installation,
//                InstallationDate = DateTimeOffset.UtcNow.AddDays(2),
//                InstallationAddress = "123 Main Street",
//                PaymentMethod = PaymentMethod.Cash,
//                Equipment = new OrderRequest.EquipmentDTO
//                {
//                    ModelName = "Test AC",
//                    ModelSource = "Warehouse",
//                    BTU = 12000,
//                    ServiceArea = 35,
//                    Price = 500,
//                    Quantity = 1
//                },
//                FullName = "Test Client",
//                PhoneNumber = "+123456789",
//                Email = "test@example.com",
//                FulfillmentStatus = FulfillmentStatus.New,
//                PaymentStatus = PaymentStatus.Unpaid,
//                WorkCost = 100,
//                TechnicianIds = new List<Guid> { Guid.NewGuid() }
//            };

//            _mockOrderServiceClient
//                .Setup(client => client.CreateOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<string>()))
//                .ReturnsAsync((CreatedOrderResponseDTO)null!);

//            // Мокируем `accessToken`
//            var cookieCollection = new Mock<IRequestCookieCollection>();
//            cookieCollection.Setup(c => c["accessToken"]).Returns("test-token");

//            // Мокируем пользователя (имитируем залогиненного менеджера)
//            var user = new ClaimsPrincipal(new ClaimsIdentity(
//            new[]
//            {
//                new Claim(ClaimTypes.NameIdentifier, managerId.ToString())
//            }, "mock"));

//            var httpContext = new DefaultHttpContext();
//            httpContext.Request.Cookies = cookieCollection.Object;
//            httpContext.User = user;
//            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

//            _testOutputHelper.WriteLine("🔄 Тест: Проверка ошибки при создании заявки...");
//            _testOutputHelper.WriteLine("🔍 accessToken: {0}", httpContext.Request.Cookies["accessToken"]);
//            _testOutputHelper.WriteLine("🔍 ManagerId: {0}", managerId);

//            // Act
//            var result = await _controller.CreateOrder(request);

//            _testOutputHelper.WriteLine("✅ Результат теста: {0}", result);

//            // Assert
//            Assert.NotNull(result);
//            var objectResult = Assert.IsType<ObjectResult>(result);
//            Assert.Equal(500, objectResult.StatusCode);
//        }

//        /// <summary>
//        /// ✅ Проверка успешного обновления статуса заказа.
//        /// </summary>
//        [Fact]
//        public async Task UpdateStatus_ReturnsOk_WhenUpdateSuccessful()
//        {
//            // Arrange
//            var updateRequest = new UpdateOrderStatusDTO { OrderId = Guid.NewGuid(), NewStatus = FulfillmentStatus.InProgress };

//            _mockOrderServiceClient
//                .Setup(client => client.UpdateOrderStatusAsync(updateRequest.OrderId, updateRequest.NewStatus, "test-token"))
//                .ReturnsAsync(true);

//            _testOutputHelper.WriteLine("🔄 Тест: Обновление статуса заказа...");

//            // Act
//            var result = await _controller.UpdateStatus(updateRequest);

//            _testOutputHelper.WriteLine("✅ Результат теста: {0}", result);

//            // Assert
//            Assert.IsType<OkObjectResult>(result);
//        }
//    }
//}