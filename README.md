# KwiatLuxe REST API

The **KwiatLuxe REST API** is a secure, token-authenticated backend system for an e-commerce application, supporting cart management, dynamic product ordering, and user-specific data access. Built with ASP.NET Web API and Entity Framework, the system is designed with maintainability and scalability in mind.

---

## Features

- **Secure Cart System**  
  - Each user can have **only one cart** (One-to-One relationship).
  - Supports cart creation, item addition/update, and deletion.
  - Real-time total cost calculation with precision updates based on item quantity.

- **Robust Order Placement**  
  - Each user can have **multiple orders** (One-to-Many relationship).
  - Orders are created from cart data, with all associated products captured via linking tables.
  - Supports viewing personal order history and cancelling specific orders.

- **JWT Authorization**  
  - All cart and order endpoints require valid authentication tokens.
  - Uses user claims to resolve identity securely.

- **Custom Logging Mechanism**  
  - Logs critical operations at key points using `Logger.Log(Severity.WARN, ...)`.
  - Can also write logs to file in `\Logs` folder.
  - Severity-based color-coded console logs
  - Toggleable debug output
---

## Implementation

### 1. **Handling One-to-One Cart Logic**
Only one cart per user is allowed. Before creating a new cart:
```csharp
var identicalUserCart = await _db.Carts.Where(c => c.UserId == userId).FirstOrDefaultAsync();
if (identicalUserCart != null) 
{
    return BadRequest(new { CartExists = $"Cart with UserId {userId} already exists." });
}
```
This guards against duplication and enforces the one-to-one relationship programmatically.

2. Dynamic Cost Recalculation for Cart Updates

When updating the quantity of an item in a cart:

- The system recalculates only the delta between new and old quantity.
- Prevents re-looping over the entire cart, improving performance and clarity.
```csharp
if (currentQuantity > targetQuantity) {
    newCost -= (newQuantity * getProduct.ProductPrice);
}
else if (currentQuantity < targetQuantity) {
    newCost += (newQuantity * getProduct.ProductPrice);
}
```
3. Cart-to-Order Transition On order placement:

Cart data is transformed into order entities.

- Products from CartProducts are converted into OrderProducts.
- The system does not clear the cart automatically, giving flexibility for future enhancement (e.g., transactional rollback or user confirmation).

```csharp
foreach (var product in getUserCart.CartProducts)
{
    var orderProduct = new OrderProduct
    {
        OrderId = order.Id,
        ProductId = product.ProductId,
        Quantity = product.Quantity
    };
    _db.OrderProducts.Add(orderProduct);
}
```
4. Explicit Deletion Logic

Both carts and orders are deleted manually along with their child product entries, preventing orphan records:
```csharp
foreach (var cartProducts in removeAllCartProducts)
{
    _db.CartProducts.Remove(cartProducts);
}
```
This avoids relying solely on cascade deletes and gives more control over clean-up logic.

## Authentication & Authorization

- **JWT-based access and refresh token system**
  - Separate `AccessToken` and `RefreshToken` lifecycle
  - `RefreshToken` persisted in the Database
- **Role-based policies** (`User`, `Admin`) and **custom policy-based claims** (`AccessToken`, `RefreshToken`)
- **Custom Token validation and logging events** like:
  - `OnTokenValidated`
  - `OnAuthenticationFailed`
  - `OnChallenge`
  - `OnForbidden`

Supports both **cookie-based** and **header-based** auth (configurable via `USE_COOKIES` flag).

## Database Structure

The application uses the following tables:
| Table	| Description|
|-------|------------|
|Users	|Stores user information.|
|Products|	Stores available products and prices.|
|Carts|	One-to-One with Users. Each user has a single cart.|
|CartProducts	|Links products to a cart (many-to-one).|
|Orders	|One-to-Many with Users. Stores all user orders.|
|OrderProducts|	Links products to an order (many-to-one).|
|Tokens|One-to-One with Users. Stores long term refresh tokens|

## Entity Relationships

    One-to-One:
    User → Carts
    User → Tokens 

    One-to-Many:
    User → Orders
    Order → OrderProducts
    Carts → CartProducts

## API Endpoints Overview
|Method|	Route|	Description|
|------|--------|--------------|
|POST|	/cart/createcart|	Creates a new cart for a user.|
POST|	/cart/addcart|	Adds or updates products in cart.|
GET	|/cart/mycart|	Gets the current user’s cart.|
DELETE|	/cart/removecart|	Deletes user’s cart and its items.|
POST|	/order/placeorder|	Converts cart into a placed order.|
GET	|/order/myorders	| Retrieves all user’s past orders.|
|DELETE|	/order/cancelorder{id}|	Cancels a specific order.|

## Tech Stack

- Backend: ASP.NET Web API
- Database: MySQL (via phpMyAdmin/XAMPP)
- Auth: JWT Bearer Tokens
- Testing Tool: Swagger UI
