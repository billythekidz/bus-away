# Grid Mapping Analysis

Dựa trên hình ảnh `grid_tiles.jpg`, lưới (grid) thực tế có kích thước là **5x6** (5 hàng ngang, 6 cột dọc). Xin lỗi vì đếm nhầm chiều dọc/ngang trước đó. Dưới đây là kết quả Mapping lại mảng 5x6 chuẩn xác nhất vào ma trận số nguyên (int) tương ứng với enum `RoadCellType`:

```csharp
int[,] mapGrid = new int[5, 6] {
    // Cột 0   Cột 1   Cột 2   Cột 3   Cột 4   Cột 5
    {   0,     11,     12,     11,     12,      0   }, // Hàng 0 (Khu Gara ngang: Xe Tím C1-C2, Xe Xanh C3-C4, dùng HalfT_BusStop_N_Left/Right)
    {   5,      2,      2,      2,      2,      6   }, // Hàng 1 (Đường ngang trên, và góc cua trên)
    {   6,      0,      0,      0,     13,      1   }, // Hàng 2 (C0: Nửa trên của góc cua trái (6), C4: Nhà chờ dọc 1 (HalfT_BusStop_E_Left))
    {   4,      0,      0,      0,     14,      1   }, // Hàng 3 (C0: Nửa dưới của góc cua trái (4), C4: Nhà chờ dọc 2 (HalfT_BusStop_E_Right))
    {   3,      2,      2,      2,      2,      4   }  // Hàng 4 (Đường ngang dưới, xe Vàng đỗ ở góc C5)
};
```
    public enum RoadCellType
    {
        Empty = 0,              // Ô trống, không có đường hay công trình nào

        Straight_NS = 1,        // Đường thẳng dọc (Bắc-Nam), xe di chuyển theo trục Z
        Straight_EW = 2,        // Đường thẳng ngang (Đông-Tây), xe di chuyển theo trục X

        Corner_NE = 3,          // Góc cua Bắc-Đông: Nối đường từ phía Bắc (trên) sang phía Đông (phải)
        Corner_NW = 4,          // Góc cua Bắc-Tây: Nối đường từ phía Bắc (trên) sang phía Tây (trái)
        Corner_SE = 5,          // Góc cua Nam-Đông: Nối đường từ phía Nam (dưới) sang phía Đông (phải)
        Corner_SW = 6,          // Góc cua Nam-Tây: Nối đường từ phía Nam (dưới) sang phía Tây (trái)

        HalfT_BusStop_N_Left = 11,      // Nửa trái của trạm Bus (nút giao T) hướng Bắc (chuồng đỗ mở ra hướng Nam)
        HalfT_BusStop_N_Right = 12,     // Nửa phải của trạm Bus (nút giao T) hướng Bắc
        HalfT_BusStop_E_Left = 13,      // Nửa trái của trạm Bus hướng Đông
        HalfT_BusStop_E_Right = 14,     // Nửa phải của trạm Bus hướng Đông
        HalfT_BusStop_S_Left = 15,      // Nửa trái của trạm Bus hướng Nam
        HalfT_BusStop_S_Right = 16,     // Nửa phải của trạm Bus hướng Nam
        HalfT_BusStop_W_Left = 17,      // Nửa trái của trạm Bus hướng Tây
        HalfT_BusStop_W_Right = 18,     // Nửa phải của trạm Bus hướng Tây
        
        Cross = 19,             // Ngã tư đầy đủ: 4 nhánh thông nhau (Bắc, Nam, Đông, Tây)

        DeadEnd_N = 20,         // Tuyến đường đâm về hướng Bắc. Phần ngõ cụt (bị bịt) nằm ở viền Bắc, cổng kết nối mở ra hướng Nam.
        DeadEnd_E = 21,         // Tuyến đường đâm về hướng Đông. Phần ngõ cụt (bị bịt) nằm ở viền Đông, cổng kết nối mở ra hướng Tây.
        DeadEnd_S = 22,         // Tuyến đường đâm về hướng Nam. Phần ngõ cụt (bị bịt) nằm ở viền Nam, cổng kết nối mở ra hướng Bắc.
        DeadEnd_W = 23,         // Tuyến đường đâm về hướng Tây. Phần ngõ cụt (bị bịt) nằm ở viền Tây, cổng kết nối mở ra hướng Đông.

        // (Loại bỏ các độc lập BusStop vì theo rule mới, ngã ba chữ T chính là trạm xe buýt)

        // Các type tổng quát, dùng khi chưa xác định loại cụ thể hoặc để test nhanh
        GenericRoad = 99,       // Đường generic, chưa phân loại hướng. Dùng tạm trong quá trình thiết kế
    }
---

## Chi tiết các Cell Type được dùng trong Grid

### `0` - Empty
- **Tên:** Empty
- **Là gì:** Ô đất trống, không có đường.
- **Đặc điểm:** Trong hình, đây là vùng nền trắng ở giữa bản đồ.
- **Làm gì:** Giới hạn không gian di chuyển, có thể được dùng để đặt các mô hình trang trí (Prop) hoặc bãi cỏ trong tương lai. Xe cộ không thể đi vào.

### `1` - Straight_NS (North - South)
- **Tên:** Đường thẳng dọc
- **Là gì:** Đoạn đường nhựa thẳng đứng.
- **Đặc điểm:** Kết nối 2 ô liền kề ở phía Bắc (trên) và Nam (dưới).
- **Làm gì:** Cho phép xe buýt di chuyển thông qua theo trục dọc. Vị trí `(3, 4)` trong ảnh đang chứa một đầu xe màu vàng đỗ trên đoạn đường này.

### `2` - Straight_EW (East - West)
- **Tên:** Đường thẳng ngang
- **Là gì:** Đoạn đường nhựa nằm ngang.
- **Đặc điểm:** Kết nối 2 ô liền kề ở phía Tây (trái) và Đông (phải). 
- **Làm gì:** Cho phép xe buýt di chuyển theo trục ngang. Ở hàng trên cùng `(0, 1)` và `(0, 2)`, vùng đường này đóng vai trò kết nối trực tiếp với các Gara xuất phát của xe buýt tím và xanh lá.

### `3` - Corner_NE (North - East)
- **Tên:** Góc cua B-Đ (Dưới-Trái)
- **Là gì:** Khúc cua phần dưới cùng bên trái của map.
- **Đặc điểm:** Có hình cung, rẽ từ trục Dọc (Bắc) sang ngang (Đông).
- **Làm gì:** Điều hướng xe buýt chuyển góc vuông 90 độ mượt mà từ đường dọc sang đường ngang.

### `4` - Corner_NW (North - West)
- **Tên:** Góc cua B-T (Dưới-Phải)
- **Là gì:** Khúc cua phần dưới cùng bên phải của map.
- **Đặc điểm:** Rẽ từ hướng Đông/Tây sang Bắc.
- **Làm gì:** Khép kín chu trình đường bên góc dưới phải.

### `5` - Corner_SE (South - East)
- **Tên:** Góc cua N-Đ (Trên-Trái)
- **Là gì:** Khúc cua trên cùng bên trái.
- **Đặc điểm:** Cua giữa hướng Nam (xuống) và Đông (phải).
- **Làm gì:** Nối mạch đường từ vành đai ngang bên trên rẽ xuống vành đai dọc bên trái.

### `4` và `6` - Góc cua chữ C bên trái (Corner_NW & Corner_SW)
- **Là gì:** Khúc rẽ trái dài ôm lấy bản đồ dọc theo cột 0.
- **Đặc điểm:** Thay vì dùng 1 ô đường thẳng đứng `1` làm điểm giữa như trước (bị sai do mép ngoài bị uốn cong lồi ra khỏi lưới thẳng), phần "phình to" ở cột 0 kết nối 2 ô Corner để tạo thành biên dạng U-turn (như trong ảnh đánh dấu X). `6` (Corner_SW) nằm trên `4` (Corner_NW) chập lại thành đường cong chữ C cho phần viền ngoài. 

### `11->18` - Cụm Nhóm BusStop và Gara (Nguyên khối theo hướng xoay)
- **Là gì:** Phần cấu hình 2 cell của trạm xe buýt hoặc khu vực gara đỗ chờ (HalfT_BusStop). Các mã số được đặt xen kẽ liền kề nhau tương ứng như enum `RoadCellType`:
  - **Hướng Bắc (N):** `11` (Nửa Trái - Left) và `12` (Nửa Phải - Right)
  - **Hướng Đông (E):** `13` (Nửa Trái - Left) và `14` (Nửa Phải - Right)
  - **Hướng Nam (S):** `15` (Nửa Trái - Left) và `16` (Nửa Phải - Right)
  - **Hướng Tây (W):** `17` (Nửa Trái - Left) và `18` (Nửa Phải - Right)
- **Đặc điểm:** Việc tách trạm ra thành 2 ô liền kề (Left/Right) giúp model trạm xe có thể toát trọn vẹn 2 grids, dễ quản lý mesh kích thước khổng lồ mà không sợ móp méo lưới.
- **Làm gì:** Định vị chính xác phương hướng và diện tích để Spawn prefab trạm chờ Bus tương ứng lên Scene. Hệ thống prefab tự động map theo đúng Enum này.
