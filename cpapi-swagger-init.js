
window.onload = function() {
  // Build a system
  var url = window.location.search.match(/url=([^&]+)/);
  if (url && url.length > 1) {
    url = decodeURIComponent(url[1]);
  } else {
    url = window.location.origin;
  }
  var options = {
  "swaggerDoc": {
    "openapi": "3.0.0",
    "info": {
      "title": "Multi Branch POS API",
      "version": "1.0.0",
      "description": "API documentation"
    },
    "servers": [
      {
        "url": "https://cpapi.jamesnguyen831.id.vn/api"
      }
    ],
    "components": {
      "securitySchemes": {
        "bearerAuth": {
          "type": "http",
          "scheme": "bearer",
          "bearerFormat": "JWT"
        }
      },
      "schemas": {
        "User": {
          "type": "object",
          "properties": {
            "id": {
              "type": "integer",
              "example": 1,
              "readOnly": true
            },
            "username": {
              "type": "string",
              "example": "admin"
            },
            "role": {
              "type": "string",
              "example": "admin"
            },
            "branchId": {
              "type": "integer",
              "example": 1
            },
            "createdAt": {
              "type": "string",
              "format": "date-time",
              "example": "2023-01-01T00:00:00Z",
              "readOnly": true
            },
            "updatedAt": {
              "type": "string",
              "format": "date-time",
              "example": "2023-01-01T00:00:00Z",
              "readOnly": true
            }
          }
        },
        "Product": {
          "type": "object",
          "properties": {
            "id": {
              "type": "integer",
              "example": 1,
              "readOnly": true
            },
            "name": {
              "type": "string",
              "example": "Cà phê sữa"
            },
            "price": {
              "type": "number",
              "format": "double",
              "example": 25000
            },
            "description": {
              "type": "string",
              "example": "Cà phê sữa đá ngọt"
            },
            "createdAt": {
              "type": "string",
              "format": "date-time",
              "example": "2023-01-01T00:00:00Z",
              "readOnly": true
            },
            "updatedAt": {
              "type": "string",
              "format": "date-time",
              "example": "2023-01-01T00:00:00Z",
              "readOnly": true
            }
          }
        },
        "Branch": {
          "type": "object",
          "properties": {
            "id": {
              "type": "integer",
              "example": 1,
              "readOnly": true
            },
            "name": {
              "type": "string",
              "example": "Chi nhánh Quận 1"
            },
            "address": {
              "type": "string",
              "example": "123 Lê Lợi, Quận 1, TP.HCM"
            },
            "createdAt": {
              "type": "string",
              "format": "date-time",
              "example": "2023-01-01T00:00:00Z",
              "readOnly": true
            },
            "updatedAt": {
              "type": "string",
              "format": "date-time",
              "example": "2023-01-01T00:00:00Z",
              "readOnly": true
            }
          }
        },
        "Customer": {
          "type": "object",
          "properties": {
            "id": {
              "type": "integer",
              "example": 1,
              "readOnly": true
            },
            "name": {
              "type": "string",
              "example": "Nguyễn Văn A"
            },
            "phone": {
              "type": "string",
              "example": "0901234567"
            },
            "email": {
              "type": "string",
              "example": "nguyenvana@example.com"
            },
            "createdAt": {
              "type": "string",
              "format": "date-time",
              "example": "2023-01-01T00:00:00Z",
              "readOnly": true
            },
            "updatedAt": {
              "type": "string",
              "format": "date-time",
              "example": "2023-01-01T00:00:00Z",
              "readOnly": true
            }
          }
        },
        "Inventory": {
          "type": "object",
          "properties": {
            "id": {
              "type": "integer",
              "example": 1,
              "readOnly": true
            },
            "productId": {
              "type": "integer",
              "example": 1
            },
            "branchId": {
              "type": "integer",
              "example": 1
            },
            "quantity": {
              "type": "integer",
              "example": 100
            },
            "createdAt": {
              "type": "string",
              "format": "date-time",
              "example": "2023-01-01T00:00:00Z",
              "readOnly": true
            },
            "updatedAt": {
              "type": "string",
              "format": "date-time",
              "example": "2023-01-01T00:00:00Z",
              "readOnly": true
            }
          }
        },
        "Order": {
          "type": "object",
          "properties": {
            "id": {
              "type": "integer",
              "example": 1,
              "readOnly": true
            },
            "staffId": {
              "type": "integer",
              "example": 1
            },
            "branchId": {
              "type": "integer",
              "example": 1
            },
            "customerId": {
              "type": "integer",
              "example": 1
            },
            "totalAmount": {
              "type": "number",
              "format": "double",
              "example": 50000
            },
            "paymentMethod": {
              "type": "string",
              "example": "cash"
            },
            "syncStatus": {
              "type": "string",
              "example": "pending"
            },
            "items": {
              "type": "array",
              "items": {
                "$ref": "#/components/schemas/OrderItem"
              }
            },
            "createdAt": {
              "type": "string",
              "format": "date-time",
              "example": "2023-01-01T00:00:00Z",
              "readOnly": true
            },
            "updatedAt": {
              "type": "string",
              "format": "date-time",
              "example": "2023-01-01T00:00:00Z",
              "readOnly": true
            }
          }
        },
        "OrderItem": {
          "type": "object",
          "properties": {
            "id": {
              "type": "integer",
              "example": 1,
              "readOnly": true
            },
            "orderId": {
              "type": "integer",
              "example": 1
            },
            "productId": {
              "type": "integer",
              "example": 1
            },
            "quantity": {
              "type": "integer",
              "example": 2
            },
            "price": {
              "type": "number",
              "format": "double",
              "example": 25000
            },
            "createdAt": {
              "type": "string",
              "format": "date-time",
              "example": "2023-01-01T00:00:00Z",
              "readOnly": true
            },
            "updatedAt": {
              "type": "string",
              "format": "date-time",
              "example": "2023-01-01T00:00:00Z",
              "readOnly": true
            }
          }
        },
        "LoginRequest": {
          "type": "object",
          "required": [
            "username",
            "password"
          ],
          "properties": {
            "username": {
              "type": "string",
              "example": "admin"
            },
            "password": {
              "type": "string",
              "example": "123456"
            }
          }
        },
        "SyncOrdersRequest": {
          "type": "object",
          "properties": {
            "orders": {
              "type": "array",
              "items": {
                "$ref": "#/components/schemas/Order"
              }
            }
          }
        },
        "SyncStatusUpdateRequest": {
          "type": "object",
          "required": [
            "orderIds",
            "status"
          ],
          "properties": {
            "orderIds": {
              "type": "array",
              "items": {
                "type": "integer"
              },
              "example": [
                1,
                2,
                3
              ]
            },
            "status": {
              "type": "string",
              "enum": [
                "synced",
                "pending",
                "failed"
              ],
              "description": "New synchronization status for the specified orders",
              "example": "synced"
            }
          }
        }
      }
    },
    "paths": {
      "/auth/login": {
        "post": {
          "summary": "User login",
          "tags": [
            "Auth"
          ],
          "requestBody": {
            "required": true,
            "content": {
              "application/json": {
                "schema": {
                  "$ref": "#/components/schemas/LoginRequest"
                }
              }
            }
          },
          "responses": {
            "200": {
              "description": "Login successful, returns JWT"
            }
          }
        }
      },
      "/auth/profile": {
        "get": {
          "summary": "Get current user profile",
          "tags": [
            "Auth"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "responses": {
            "200": {
              "description": "User profile information"
            }
          }
        }
      },
      "/auth/me": {
        "get": {
          "summary": "Get current user details (alias of profile)",
          "tags": [
            "Auth"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "responses": {
            "200": {
              "description": "Current authenticated user details"
            }
          }
        }
      },
      "/auth/logout": {
        "post": {
          "summary": "User logout",
          "tags": [
            "Auth"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "responses": {
            "200": {
              "description": "Logout successful"
            }
          }
        }
      },
      "/branches": {
        "get": {
          "summary": "Get all branches",
          "tags": [
            "Branches"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "responses": {
            "200": {
              "description": "List of all branches"
            }
          }
        },
        "post": {
          "summary": "Create a new branch (Admin only)",
          "tags": [
            "Branches"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "requestBody": {
            "required": true,
            "content": {
              "application/json": {
                "schema": {
                  "$ref": "#/components/schemas/Branch"
                }
              }
            }
          },
          "responses": {
            "201": {
              "description": "Branch created"
            }
          }
        }
      },
      "/branches/{id}": {
        "get": {
          "summary": "Get branch by ID",
          "tags": [
            "Branches"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "parameters": [
            {
              "in": "path",
              "name": "id",
              "required": true,
              "schema": {
                "type": "integer"
              }
            }
          ],
          "responses": {
            "200": {
              "description": "Branch details"
            }
          }
        },
        "put": {
          "summary": "Update branch (Admin only)",
          "tags": [
            "Branches"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "parameters": [
            {
              "in": "path",
              "name": "id",
              "required": true,
              "schema": {
                "type": "integer"
              }
            }
          ],
          "requestBody": {
            "required": true,
            "content": {
              "application/json": {
                "schema": {
                  "$ref": "#/components/schemas/Branch"
                }
              }
            }
          },
          "responses": {
            "200": {
              "description": "Branch updated"
            }
          }
        },
        "delete": {
          "summary": "Delete branch (Admin only)",
          "tags": [
            "Branches"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "parameters": [
            {
              "in": "path",
              "name": "id",
              "required": true,
              "schema": {
                "type": "integer"
              }
            }
          ],
          "responses": {
            "200": {
              "description": "Branch deleted"
            }
          }
        }
      },
      "/customers": {
        "get": {
          "summary": "Get all customers",
          "tags": [
            "Customers"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "responses": {
            "200": {
              "description": "List of all customers"
            }
          }
        },
        "post": {
          "summary": "Create a new customer",
          "tags": [
            "Customers"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "requestBody": {
            "required": true,
            "content": {
              "application/json": {
                "schema": {
                  "$ref": "#/components/schemas/Customer"
                }
              }
            }
          },
          "responses": {
            "201": {
              "description": "Customer created"
            }
          }
        }
      },
      "/customers/{id}": {
        "get": {
          "summary": "Get customer by ID",
          "tags": [
            "Customers"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "parameters": [
            {
              "in": "path",
              "name": "id",
              "required": true,
              "schema": {
                "type": "integer"
              }
            }
          ],
          "responses": {
            "200": {
              "description": "Customer details"
            }
          }
        },
        "put": {
          "summary": "Update customer (Admin only)",
          "tags": [
            "Customers"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "parameters": [
            {
              "in": "path",
              "name": "id",
              "required": true,
              "schema": {
                "type": "integer"
              }
            }
          ],
          "requestBody": {
            "required": true,
            "content": {
              "application/json": {
                "schema": {
                  "$ref": "#/components/schemas/Customer"
                }
              }
            }
          },
          "responses": {
            "200": {
              "description": "Customer updated"
            }
          }
        },
        "delete": {
          "summary": "Delete customer (Admin only)",
          "tags": [
            "Customers"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "parameters": [
            {
              "in": "path",
              "name": "id",
              "required": true,
              "schema": {
                "type": "integer"
              }
            }
          ],
          "responses": {
            "200": {
              "description": "Customer deleted"
            }
          }
        }
      },
      "/dashboard/stats": {
        "get": {
          "summary": "Get dashboard statistics and analytics",
          "tags": [
            "Dashboard"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "parameters": [
            {
              "in": "query",
              "name": "branchId",
              "schema": {
                "type": "string"
              },
              "description": "Branch ID to filter statistics, or \"all\" to retrieve stats across all branches (admin only)"
            }
          ],
          "responses": {
            "200": {
              "description": "Dashboard statistics retrieved successfully",
              "content": {
                "application/json": {
                  "schema": {
                    "type": "object",
                    "properties": {
                      "success": {
                        "type": "boolean"
                      },
                      "data": {
                        "type": "object",
                        "properties": {
                          "totalRevenue": {
                            "type": "number"
                          },
                          "totalOrders": {
                            "type": "integer"
                          },
                          "lowStockCount": {
                            "type": "integer"
                          },
                          "totalProducts": {
                            "type": "integer"
                          },
                          "recentOrders": {
                            "type": "array",
                            "items": {
                              "$ref": "#/components/schemas/Order"
                            }
                          },
                          "revenueByBranch": {
                            "type": "array",
                            "items": {
                              "type": "object",
                              "properties": {
                                "branchId": {
                                  "type": "integer"
                                },
                                "revenue": {
                                  "type": "number"
                                },
                                "Branch": {
                                  "type": "object",
                                  "properties": {
                                    "name": {
                                      "type": "string"
                                    }
                                  }
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      },
      "/inventories": {
        "get": {
          "summary": "Get all inventories",
          "tags": [
            "Inventories"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "responses": {
            "200": {
              "description": "List of all inventories"
            }
          }
        },
        "post": {
          "summary": "Create a new inventory record (Admin only)",
          "tags": [
            "Inventories"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "requestBody": {
            "required": true,
            "content": {
              "application/json": {
                "schema": {
                  "$ref": "#/components/schemas/Inventory"
                }
              }
            }
          },
          "responses": {
            "201": {
              "description": "Inventory record created"
            }
          }
        }
      },
      "/inventories/{id}": {
        "get": {
          "summary": "Get inventory by ID",
          "tags": [
            "Inventories"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "parameters": [
            {
              "in": "path",
              "name": "id",
              "required": true,
              "schema": {
                "type": "integer"
              }
            }
          ],
          "responses": {
            "200": {
              "description": "Inventory details"
            }
          }
        },
        "put": {
          "summary": "Update inventory record (Admin only)",
          "tags": [
            "Inventories"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "parameters": [
            {
              "in": "path",
              "name": "id",
              "required": true,
              "schema": {
                "type": "integer"
              }
            }
          ],
          "requestBody": {
            "required": true,
            "content": {
              "application/json": {
                "schema": {
                  "$ref": "#/components/schemas/Inventory"
                }
              }
            }
          },
          "responses": {
            "200": {
              "description": "Inventory record updated"
            }
          }
        },
        "delete": {
          "summary": "Delete inventory record (Admin only)",
          "tags": [
            "Inventories"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "parameters": [
            {
              "in": "path",
              "name": "id",
              "required": true,
              "schema": {
                "type": "integer"
              }
            }
          ],
          "responses": {
            "200": {
              "description": "Inventory record deleted"
            }
          }
        }
      },
      "/orders": {
        "get": {
          "summary": "Get all orders (Admin only)",
          "tags": [
            "Orders"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "responses": {
            "200": {
              "description": "List of all orders"
            },
            "403": {
              "description": "Forbidden"
            }
          }
        },
        "post": {
          "summary": "Create a new order",
          "tags": [
            "Orders"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "requestBody": {
            "required": true,
            "content": {
              "application/json": {
                "schema": {
                  "$ref": "#/components/schemas/Order"
                }
              }
            }
          },
          "responses": {
            "201": {
              "description": "Order created"
            }
          }
        }
      },
      "/orders/history": {
        "get": {
          "summary": "Get order history (Admin or Staff)",
          "tags": [
            "Orders"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "responses": {
            "200": {
              "description": "List of orders filtered by branch for staff"
            }
          }
        }
      },
      "/orders/next-id": {
        "get": {
          "summary": "Get the next globally available order ID",
          "tags": [
            "Orders"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "responses": {
            "200": {
              "description": "Next order ID"
            },
            "403": {
              "description": "Forbidden"
            }
          }
        }
      },
      "/orders/{id}": {
        "get": {
          "summary": "Get order by ID",
          "tags": [
            "Orders"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "parameters": [
            {
              "in": "path",
              "name": "id",
              "required": true,
              "schema": {
                "type": "integer"
              }
            }
          ],
          "responses": {
            "200": {
              "description": "Order details"
            }
          }
        },
        "delete": {
          "summary": "Delete order (Admin only)",
          "tags": [
            "Orders"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "parameters": [
            {
              "in": "path",
              "name": "id",
              "required": true,
              "schema": {
                "type": "integer"
              }
            }
          ],
          "responses": {
            "200": {
              "description": "Order deleted"
            }
          }
        }
      },
      "/products": {
        "get": {
          "summary": "Get all products",
          "tags": [
            "Products"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "responses": {
            "200": {
              "description": "List of all products"
            }
          }
        },
        "post": {
          "summary": "Create a new product (Admin only)",
          "tags": [
            "Products"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "requestBody": {
            "required": true,
            "content": {
              "application/json": {
                "schema": {
                  "$ref": "#/components/schemas/Product"
                }
              }
            }
          },
          "responses": {
            "201": {
              "description": "Product created"
            }
          }
        }
      },
      "/products/{id}": {
        "get": {
          "summary": "Get product by ID",
          "tags": [
            "Products"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "parameters": [
            {
              "in": "path",
              "name": "id",
              "required": true,
              "schema": {
                "type": "integer"
              }
            }
          ],
          "responses": {
            "200": {
              "description": "Product details"
            }
          }
        },
        "put": {
          "summary": "Update product (Admin only)",
          "tags": [
            "Products"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "parameters": [
            {
              "in": "path",
              "name": "id",
              "required": true,
              "schema": {
                "type": "integer"
              }
            }
          ],
          "requestBody": {
            "required": true,
            "content": {
              "application/json": {
                "schema": {
                  "$ref": "#/components/schemas/Product"
                }
              }
            }
          },
          "responses": {
            "200": {
              "description": "Product updated"
            }
          }
        },
        "delete": {
          "summary": "Delete product (Admin only)",
          "tags": [
            "Products"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "parameters": [
            {
              "in": "path",
              "name": "id",
              "required": true,
              "schema": {
                "type": "integer"
              }
            }
          ],
          "responses": {
            "200": {
              "description": "Product deleted"
            }
          }
        }
      },
      "/sync/orders": {
        "post": {
          "summary": "Sync orders from remote/local",
          "tags": [
            "Sync"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "requestBody": {
            "required": true,
            "content": {
              "application/json": {
                "schema": {
                  "$ref": "#/components/schemas/SyncOrdersRequest"
                }
              }
            }
          },
          "responses": {
            "200": {
              "description": "Sync successful",
              "content": {
                "application/json": {
                  "schema": {
                    "type": "array",
                    "items": {
                      "$ref": "#/components/schemas/Order"
                    }
                  }
                }
              }
            }
          }
        }
      },
      "/sync/status": {
        "get": {
          "summary": "Get synchronization status",
          "tags": [
            "Sync"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "responses": {
            "200": {
              "description": "Current sync status",
              "content": {
                "application/json": {
                  "schema": {
                    "type": "object",
                    "properties": {
                      "pending": {
                        "type": "integer"
                      },
                      "synced": {
                        "type": "integer"
                      },
                      "failed": {
                        "type": "integer"
                      }
                    }
                  }
                }
              }
            }
          }
        },
        "put": {
          "summary": "Update synchronization status for selected orders",
          "tags": [
            "Sync"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "requestBody": {
            "required": true,
            "content": {
              "application/json": {
                "schema": {
                  "$ref": "#/components/schemas/SyncStatusUpdateRequest"
                }
              }
            }
          },
          "responses": {
            "200": {
              "description": "Sync status updated successfully",
              "content": {
                "application/json": {
                  "schema": {
                    "type": "object",
                    "properties": {
                      "message": {
                        "type": "string"
                      },
                      "data": {
                        "type": "object",
                        "properties": {
                          "success": {
                            "type": "boolean"
                          },
                          "updatedCount": {
                            "type": "integer"
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      },
      "/sync/pending": {
        "get": {
          "summary": "Get pending synchronization orders",
          "tags": [
            "Sync"
          ],
          "security": [
            {
              "bearerAuth": []
            }
          ],
          "responses": {
            "200": {
              "description": "Pending orders retrieved successfully",
              "content": {
                "application/json": {
                  "schema": {
                    "type": "object",
                    "properties": {
                      "message": {
                        "type": "string"
                      },
                      "data": {
                        "type": "array",
                        "items": {
                          "type": "object",
                          "properties": {
                            "id": {
                              "type": "integer"
                            },
                            "staffId": {
                              "type": "integer"
                            },
                            "branchId": {
                              "type": "integer"
                            },
                            "customerId": {
                              "type": "integer"
                            },
                            "paymentMethod": {
                              "type": "string"
                            },
                            "totalAmount": {
                              "type": "number"
                            },
                            "createdAt": {
                              "type": "string",
                              "format": "date-time"
                            },
                            "items": {
                              "type": "array",
                              "items": {
                                "$ref": "#/components/schemas/OrderItem"
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
    },
    "tags": [
      {
        "name": "Auth",
        "description": "Authentication management"
      },
      {
        "name": "Branches",
        "description": "Branch management"
      },
      {
        "name": "Customers",
        "description": "Customer management"
      },
      {
        "name": "Dashboard",
        "description": "Dashboard analytics and statistics"
      },
      {
        "name": "Inventories",
        "description": "Inventory management"
      },
      {
        "name": "Orders",
        "description": "Order management"
      },
      {
        "name": "Products",
        "description": "Product management"
      },
      {
        "name": "Sync",
        "description": "Data synchronization"
      }
    ]
  },
  "customOptions": {}
};
  url = options.swaggerUrl || url
  var urls = options.swaggerUrls
  var customOptions = options.customOptions
  var spec1 = options.swaggerDoc
  var swaggerOptions = {
    spec: spec1,
    url: url,
    urls: urls,
    dom_id: '#swagger-ui',
    deepLinking: true,
    presets: [
      SwaggerUIBundle.presets.apis,
      SwaggerUIStandalonePreset
    ],
    plugins: [
      SwaggerUIBundle.plugins.DownloadUrl
    ],
    layout: "StandaloneLayout"
  }
  for (var attrname in customOptions) {
    swaggerOptions[attrname] = customOptions[attrname];
  }
  var ui = SwaggerUIBundle(swaggerOptions)

  if (customOptions.oauth) {
    ui.initOAuth(customOptions.oauth)
  }

  if (customOptions.preauthorizeApiKey) {
    const key = customOptions.preauthorizeApiKey.authDefinitionKey;
    const value = customOptions.preauthorizeApiKey.apiKeyValue;
    if (!!key && !!value) {
      const pid = setInterval(() => {
        const authorized = ui.preauthorizeApiKey(key, value);
        if(!!authorized) clearInterval(pid);
      }, 500)

    }
  }

  if (customOptions.authAction) {
    ui.authActions.authorize(customOptions.authAction)
  }

  window.ui = ui
}
