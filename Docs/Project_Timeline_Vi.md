# Bus Away - Project Timeline & Evolution

Đây là tài liệu được tổng hợp chi tiết theo trình tự thời gian từ cũ đến mới, đồng bộ hóa giữa lịch sử **Commits** và danh sách các **GitHub Issues** của dự án "Bus Away". Quá trình phát triển được chia làm các giai đoạn (Phases) tương ứng với từng cột mốc kiến trúc và tính năng.

---

## Phase 1: Project Setup, Architecture & Foundation (April 09, 2026)
*Giai đoạn thiết lập cơ sở hạ tầng kiến trúc dữ liệu và khả năng sinh lưới (Grid) cơ bản.*

### Step 1: Khởi tạo và thiết lập dự án
- Dự án được bắt đầu bằng những commit khởi tạo repo.
- Thiết lập `README.md` mới thay thế mặc định của GitHub, cập nhật Workspace Setting cho VSCode.
- **Issue #1 (Closed):** Sửa lỗi URP MSAA 'Missing resolve surface' rác log trên Editor do `CharacterCustomizationWindow`.
- **Issue #2 (Closed):** Sửa lỗi lighting trong scene `Cartoon City` cho khớp với reference ban đầu của Asset.

### Step 2: Kiến trúc Data-Driven & Hỗ trợ công cụ AI
- **Issue #4 (Closed):** Xây dựng **Data-driven Level Generation System**. Đưa ra kiến trúc lưu trữ dữ liệu màn chơi bằng ScriptableObjects, cho phép scale sau này.
- Implement khả năng tự động lưu script state, thiết lập project rules và mcp-skill phục vụ sự phối hợp với trợ lý AI.

### Step 3: Road Generation (Trình tạo đường 3D)
- **Issue #3 (Closed):** Ra mắt **Dynamic Road Generator**.
- Quá trình xử lý rất nhiều lỗi về hình học và kết nối mảng (grid):
  - Khắc phục xoay Pivot, lệch Scale, fix nối khớp chữ T (T-junction).
  - Tái tạo lại toàn bộ mesh cho Đường Thẳng (Straight), Ngã Tư (Cross), Góc Cua (Corner) và T-Junction bằng công cụ **ProBuilder** để đạt độ chính xác (Precision) và clear geometry artifacts tuyệt đối.
  - Sử dụng cung tròn đa giác (polygon arcs) bo góc cho đường Corner, chỉnh sửa lại thuật toán random lưới đảm bảo luôn tạo ra các cung đường khép kín (closed loops).
  - Bổ sung Prefab "Hollow" cho bến xe (`Tile_BusStop`) để tránh bị clipping với xe Bus.
  - Sửa lỗi lệch hướng Grid view trên Editor cho khớp với Game view.

### Step 4: Khởi đầu hệ thống AI Đám đông (Crowd System)
- **Epic #5, Tasks #6, #7, #8, #9 (Closed):** Hệ thống mô phỏng đám đông (High-Performance Crowd Simulation System) với sự hỗ trợ của **Job System, Burst, Collections và Mathematics**.
- Setup `NativeArrays`, `IJobParallelFor` xử lý va chạm Boids (Separation) và dùng `RenderMeshInstanced` để render hàng loạt với hiệu năng cao.

---

## Phase 2: Nâng cấp Sinh màn chơi & Nhóm Khách Hàng (April 10, 2026)
*Giai đoạn thuật toán sinh bản đồ được nâng cấp từ "vuông vắn" sang "tự nhiên", đồng thời triển khai các bãi chứa khách (Crowd Lands).*

### Step 1: Nâng cấp thuật toán Generator - BSP & Organic
- **Epic #10 & Tasks #11-#14 (Open):** Kế hoạch chuyển đổi Map Generator thành giải pháp **BSP (Binary Space Partitioning)** cắt bản đồ kết hợp **Grid Decimation** tạo ra các đường nhánh phức tạp nhưng vẫn đảm bảo tính khép kín.
- **Issue #22 (Closed):** Cuối ngày, đã thay thế toàn bộ logic sinh map bằng thuật toán tiến bộ hơn: **Organic Level Generator (L-System Growth)** giúp các con đường trông cong uốn lượn tư nhiên nhất có thể.

### Step 2: Gỡ lỗi và Hoàn thiện Crowd System
- **Issue #15 (Closed):** Khắc phục dứt điểm 7 bugs trong Crowd System tìm được sau buổi Review.
- Sửa lỗi di chuyển các tile meshes bị chồng chéo, thêm các mesh "Half T" để xử lý các điểm kết nối cụt trên giao lộ.

### Step 3: Triển khai Crowd Land System
- **Epic #16, Tasks #17-#19 (Closed/Open):** Phát triển **Crowd Land System**. 
  - Khởi tạo Data Model & Editor UI, cho phép tùy chỉnh màu sắc, số lượng các nhóm hành khách đợi xe theo bảng màu (Color Palette) tự chọn.
  - Tích hợp `CrowdManager.SpawnLand()` kích hoạt tự động theo hệ thống sinh level.

### Step 4: Logic Di Chuyển của Xe Bus (Bus V2) & Góc Quanh (Notches)
- **Issue #20 (Closed):** Triển khai Script `BusController.cs` phiên bản V2.
- **Issue #21 (Closed):** Refactor lại việc nhận dạng đường nhánh bằng thuật toán (S-Curve & Combo-5 Corners).

### Step 5: Quản Lý Hệ Thống (Game Manager)
- Implement `GameManager.cs` để bắt đầu móc nối (hook) Data của Level, Bus và Crowd lại làm một trải nghiệm luân chuyển mượt mà.

---

## Phase 3: Hoàn thiện Game Loop, Đánh bóng & SFX (April 11, 2026)
*Giai đoạn ráp nối mọi thứ thành một vòng lặp sự kiện (Game Loop) hoàn chỉnh, bổ sung tương tác chạm, Animation, Âm thanh & VFX, biến dự án thành Full-Playable Game.*

### Step 1: Level Design & Game Logic Loop
- Cấu hình hoàn thiện `Level 1 SO` (Scriptable Object).
- Render `TextMeshPro` đếm số chỗ đợi / số xe vào điểm dừng.
- Logic xe Bus đã biết chạy bo theo hình vòng lặp (Road Loop). Nhận diện xe, dừng và đi.

### Step 2: Cải tiến Crowd Agent 
- Smoothed Crowd Agents, giải quyết các lỗi va chạm hiển thị, thêm mô hình thiết kế tối giản "simple human" dựng bằng ProBuilder đẹp mắt hơn.

### Step 3: Tương tác người dùng & Đánh bóng (Polishing)
- Hoàn thiện luồng Manager.
- Tích hợp **Haptic Feedback** (độ rung) khi tương tác màn hình.
- Thêm các Hiệu ứng hình ảnh (VFX) như tia lửa/sáng khi Bus di chuyển hay bắt khách.
- Cập nhật số liệu hiển thị trong game (Game Label/Text).

### Step 4: Âm thanh & Hành vi cuối Game (Audio & Victory Logic)
- Khởi tạo Audio sources, chạy script Python sinh tự động tạo 07 file **SFX (Sound Effects)** độc đáo (tiếng thắng xe, thắng giải, đồng xu, nhảy lên ghế...).
- Game Manager được hoàn chỉnh hàm Play / Game Over do hết giờ.
- Xử lý UX: Các xe bus tự động né trùng màu tại bến.
- Tinh chỉnh Game Mode: Sau khi chạm ngưỡng thắng lơi (Win), xe Bus tiếp tục chạy "diễu hành" về đích tự nhiên để lại ấn tượng dễ chịu (Victory Drive) thay vì đứng yên lập tức.

---

**→ Tổng kết:** "Bus Away" đã trải qua một tiến trình phát triển từ Core Grid System => Dynamic Asset Generation => Highly Optimized Systems (Burst/Jobs) => Và chạm ngưỡng hoàn tất Gameplay Loop (UI, Audio, VFX, Haptics).
