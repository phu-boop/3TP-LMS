# Quy trình Quản lý Ticket Hỗ trợ (Support Ticket Workflow)

Tài liệu này mô tả quy trình xử lý ticket hỗ trợ giữa **School Client** (người dùng) và **Tenant Admin** (quản trị viên hệ thống).

## 1. Cấu trúc Module Tickets

```
src/Modules/Tickets/
├── Aig.Lms.Modules.Tickets.Api/                  # (chưa khởi tạo)
├── Aig.Lms.Modules.Tickets.Application/           # (chưa khởi tạo)
├── Aig.Lms.Modules.Tickets.Domain/
│   ├── Aig.Lms.Modules.Tickets.Domain.csproj
│   ├── Entities/
│   │   ├── Ticket.cs                              # Aggregate Root: ticket chính
│   │   ├── TicketAttachment.cs                    # Ảnh/file đính kèm
│   │   └── TicketStatus.cs                        # Enum trạng thái (NEW, IN_PROGRESS, RESOLVED)
│   ├── Events/
│   │   └── TicketEvents.cs                        # Domain Events: Created, Resolved
│   └── Repositories/
│       └── ITicketRepository.cs                   # Interface Repository
└── Aig.Lms.Modules.Tickets.Infrastructure/
    ├── Aig.Lms.Modules.Tickets.Infrastructure.csproj
    ├── DependencyInjection.cs                     # DI Registration
    └── Persistence/
        ├── TicketsDbContext.cs                    # DB Context (Dapper)
        └── Repositories/
            └── TicketRepository.cs                # Implement Repository
```

## 2. Tổng quan
Hệ thống hỗ trợ ticket giúp đảm bảo mọi sự cố hoặc yêu cầu từ School Client được tiếp nhận, phân loại, xử lý và phản hồi một cách minh bạch, có lưu vết.

## 3. Luồng thực hiện

### Bước 1: School Client tạo Ticket
- **Hành động:** School Client truy cập vào menu "Hỗ trợ" (Support) và tạo ticket mới.
- **Thông tin:** Nhập tiêu đề và mô tả chi tiết vấn đề.
- **Trạng thái:** Ticket được khởi tạo với trạng thái **Mới (New)**.
- **Thông báo:** Hệ thống tự động gửi email thông báo cho Tenant Admin quản lý Tenant đó. Email bao gồm:
    - Tên School Client.
    - Tiêu đề lỗi.
    - Thời gian gửi.
    - Link truy cập nhanh vào chi tiết ticket.

### Bước 2: Tenant Admin tiếp nhận & Xử lý
- **Hành động:** Tenant Admin truy cập menu "Quản lý Ticket".
- **Phạm vi:** Tenant Admin chỉ xem và xử lý được các ticket thuộc các School Client trong Tenant mình quản lý.
- **Tiếp nhận:** Tenant Admin mở ticket và chuyển trạng thái từ **Mới (New)** sang **Đang xử lý (In Progress)**.

### Bước 3: Giải quyết & Phản hồi
- **Hành động:** Sau khi xử lý xong, Tenant Admin:
    - Ghi chú kết quả xử lý (Resolution Note).
    - Chuyển trạng thái ticket sang **Đã giải quyết (Resolved)**.
- **Thông báo:** Hệ thống tự động gửi email phản hồi cho School Client. Nội dung bao gồm kết quả xử lý từ Tenant Admin.

## 3. Quy tắc Workflow
- **Trạng thái bắt buộc:** `Mới (New)` → `Đang xử lý (In Progress)` → `Đã giải quyết (Resolved)`.
- **Quyền hạn:** Chỉ Tenant Admin có quyền chuyển trạng thái sang `Đã giải quyết`.
- **Trigger Email:**
    - **Email 1 (Nhận):** Chỉ gửi khi ticket mới được tạo.
    - **Email 2 (Gửi đi):** Chỉ gửi khi ticket chuyển sang trạng thái `Resolved`.
- **Bảo mật:** Nội dung email không chứa mật khẩu hoặc thông tin nhạy cảm.
