# Hệ Thống Đăng Nhập và Xác Thực

## 📋 Tổng Quan

Hệ thống đăng nhập mới với phân biệt vai trò Admin và Shop Manager.

---

## 🔐 Cấu Trúc

### 1. **Login Page** (`LoginPage.jsx`)

Trang đăng nhập với:
- ✅ Input field để nhập tên user (ExternalRef)
- ✅ Danh sách demo users (quick login buttons)
- ✅ Hiển thị vai trò của mỗi user
- ✅ Auto login khi chọn demo user

**Demo Users:**
- `ADMIN_USER` - Admin (đỏ)
- `SHOP_MANAGER_01/02/03` - Shop Manager (xanh)
- `USER_DEMO` - End User (xanh nhạt)

**Flow:**
```
1. Người dùng nhập ExternalRef hoặc chọn demo user
2. POST /api/v1/users/resolve
3. Lưu user info vào localStorage
4. Redirect sang Dashboard
5. Nếu không đăng nhập → redirect sang Login
```

### 2. **Protected Routes** (`ProtectedRoute.jsx`)

Auth guards để bảo vệ routes:

```jsx
<ProtectedRoute>           // Cần đăng nhập
<AdminRoute>               // Cần Admin
<ShopManagerRoute>         // Cần Admin hoặc ShopManager
```

**Hành chỉ:**
- Chưa đăng nhập → Redirect `/login`
- Admin route + ShopManager user → Redirect `/`
- ShopManager route + EndUser → Redirect `/`

### 3. **Updated App.jsx**

Routes:
```
GET /login                 → LoginPage
GET /*                     → Protected (Layout + child pages)
```

### 4. **Updated Layout.jsx**

Header simplification:
- Hiển thị user name + role_label
- Click avatar → Show logout button
- Logout → Clear localStorage + Redirect `/login`

### 5. **API Updates** (Program.cs)

Endpoints trả về Role:
- `GET /api/v1/users/resolve` - Trả về: Id, ExternalRef, **Role**, PreferredLanguage
- `GET /api/v1/users/{userId}` - Trả về: Id, ExternalRef, **Role**, PreferredLanguage
- `GET /api/v1/users` - Trả về: Id, ExternalRef, **Role**, PreferredLanguage

---

## 🧭 User Journey

### Scenario 1: Admin Login
```
1. Vào http://localhost:5174
2. Redirect → /login (vì chưa đăng nhập)
3. Click "ADMIN_USER" button
4. POST /users/resolve { externalRef: "ADMIN_USER" }
5. Nhận user + Role: "Admin"
6. Lưu localStorage + Redirect → /
7. Hiển thị Dashboard với full permissions
```

### Scenario 2: Shop Manager Login  
```
1. Vào /login
2. Click "SHOP_MANAGER_01"
3. POST /users/resolve + nhận Role: "ShopManager"
4. Hiển thị Dashboard (chỉ thấy menu của ShopManager)
```

### Scenario 3: Custom User
```
1. Nhập custom ExternalRef (ví dụ: "CUSTOM_USER")
2. System auto-create user với Role: "EndUser" (default)
3. Login thành công
```

---

## 🔄 Authentication Flow

```
┌─────────────────────────────────────────┐
│  User visits http://localhost:5174      │
└────────────┬────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────┐
│  Check localStorage.currentUser         │
└────────────┬────────────────────────────┘
             │
    ┌────────┴────────┐
    │                 │
    ▼ No              ▼ Yes
┌─────────┐      ┌──────────────────┐
│ /login  │      │ ProtectedRoute   │
│ Page    │      │ ✓ Render Layout  │
└─────────┘      └──────────────────┘
    │
    │ User inputs ExternalRef
    ▼
┌──────────────────────────────────┐
│ POST /api/v1/users/resolve       │
│ { externalRef, preferredLanguage │
└──────────────────────────────────┘
    │
    ▼
┌──────────────────────────────────────┐
│ User resolved/created with Role      │
│ Save to localStorage {               │
│   id, externalRef, role              │
│ }                                    │
└──────────────────────────────────────┘
    │
    ▼
┌──────────────────────────┐
│ Redirect → Dashboard     │
│ with user profile        │
└──────────────────────────┘
    │
    ▼
┌──────────────────────────────┐
│ User clicks Logout           │
│ Clear localStorage           │
│ Redirect → /login            │
└──────────────────────────────┘
```

---

## 🎯 Features by Role

### Admin (ADMIN_USER)
- ✅ View/Edit all POIs
- ✅ Manage all users
- ✅ View all analytics
- ✅ Approve/Reject content
- ✅ Manage shop verification
- ✅ Should have: /admin panel

### Shop Manager (SHOP_MANAGER_01/02/03)
- ✅ Chỉ xem POI của shop mình
- ✅ Create/Edit/Delete POI trong shop
- ✅ Manage audio + TTS config
- ✅ Create/Submit content for approval
- ✅ View shop-specific analytics
- ✅ Should have: /shops/manage panel

### End User (USER_DEMO, others)
- ✅ View live content (POI, tours)
- ✅ Listen to audio
- ✅ View subscriptions
- ❌ Không thể edit content
- ❌ Không thể create POI

---

## 📝 Files Created/Modified

### Created
- ✅ `src/pages/LoginPage.jsx` - Login page with demo users
- ✅ `src/components/ProtectedRoute.jsx` - Auth guards

### Modified
- ✅ `src/App.jsx` - Added /login route, wrapped routes with ProtectedRoute
- ✅ `src/components/Layout.jsx` - Simplified user menu, added logout redirect
- ✅ `VinhKhanhAudioGuide.Api/Program.cs` - Updated endpoints to return Role

---

## 🧪 Testing

### Test Case 1: Admin Login
```bash
# 1. Visit http://localhost:5174/login
# 2. Click ADMIN_USER
# 3. Should redirect to / with "Quản trị viên" in header
# 4. Should see all menu items
# 5. Click avatar → Click Logout → Should redirect to /login
```

### Test Case 2: Shop Manager Rights
```bash
# 1. Login as SHOP_MANAGER_01
# 2. Should see "Quản lý cửa hàng" in header
# 3. Navigate to pages → Should work (no restriction on frontend yet)
# 4. API calls with X-User-Id header should enforce shop scoping
```

### Test Case 3: Session Persistence
```bash
# 1. Login as ADMIN_USER
# 2. Refresh page (F5)
# 3. Should stay logged in (not redirect to login)
# 4. localStorage should have currentUser data
# 5. Clear localStorage → Refresh → Redirect to login
```

### Test Case 4: Direct URL Access
```bash
# 1. Without login, visit http://localhost:5174/pois
# 2. Should redirect to /login
# 3. After login, visit /pois → Should work
```

---

## 🚀 Next Steps

1. **Role-Based UI** - Hide/show menu items based on role
   - Admin: Enable admin panel
   - ShopManager: Enable shops management
   - EndUser: Show only view pages

2. **Shop Manager Pages** - Create dedicated pages
   - `/shops/manage` - Shop profile + POI list
   - `/shops/{shopId}/content` - Content CRUD
   - `/shops/{shopId}/audio` - Audio management
   - `/shops/{shopId}/analytics` - Shop analytics

3. **Admin Panel** - Create admin-only pages
   - `/admin/shops` - Verify shops
   - `/admin/users` - Manage users
   - `/admin/approvals` - Content approval queue

4. **Email Verification** (optional)
   - Implement real email/password login
   - 2FA for admin accounts

---

## 🔗 Related Files

- Backend: `VinhKhanhAudioGuide.Backend/Domain/Enums/UserRole.cs`
- Backend: `VinhKhanhAudioGuide.Backend/Domain/Entities/User.cs`
- Backend: `VinhKhanhAudioGuide.Backend/Infrastructure/DataSeeder.cs`
- Frontend: `src/contexts/UserContext.jsx`
- Frontend: `src/services/apiClient.js`
