# Thuật Toán Tạo Bản Đồ: 1D Sequence Extrusion (Path Rerouting)

Tài liệu này ghi chú chi tiết về sự chuyển đổi từ thuật toán "Carving & Auto-tiling" (Cũ) sang "1D Sequence Extrusion" (Mới) áp dụng trong lõi thiết kế `LevelDesignDataEditor.cs`. Giúp khắc phục nhược điểm "không tạo rẽ sát vách" – giải quyết bài toán layout chặt chẽ ("đồng hồ cát") đặc trưng của dòng game.

## 1. Vấn Đề Của Thuật Toán Cũ (Grid Carving & Auto-Tiling)
- **Phương pháp cũ:** Điền một khối `GenericRoad` hoặc đào lỗ để tạo thành cung đường. Sau đó gọi hàm `UpdateAllRoadTypes` để tính mask (auto-tiling).
- **Hạn chế:** Thuật toán auto-tiling hoạt động theo nguyên tắc *Flood-fill / Adjacent Matching* – cứ nhìn thấy cell đường nào bên cạnh là tự động cầu nối xuyên qua nhau (tạo T-Junction, ngã tư Cross). 
- **Hậu quả:** 
  - Hai cung đường rẽ góc (Corner) nếu được đặt nằm sát nhau để tạo độ thắt (ví dụ dáng vòng số 8, đồng hồ cát) sẽ lập tức bị thuật toán auto-tiling cưỡng ép nối lại, làm gãy mạch 1 chiều. 
  - Hàm check `IsPerfectLoop` (đếm Neighbor của mỗi Road phải đúng bằng 2) liên tục báo lỗi do các nút gộp. Thuật toán fallback liên tục, dẫn tới việc chỉ sinh ra các loop rời rạc không thể tối ưu hóa không gian chật hẹp, hoặc không sinh ra được.

## 2. Giải Pháp: 1D Sequence Extrusion
Thay vì đẽo gọt một mảng 2D Boolean và cố gắng dự đoán xem đường nó rẽ đi đâu, thuật toán mới quản lý **định hướng chuỗi 1 chiều** ngay từ đầu và chủ động tự hardcode Tọa Độ.

### Bước 1: Khởi nguồn đường vòng cơ bản (Base Loop)
Bắt đầu với một cụm đường móng hình chữ nhật tối giản (ví dụ 4 cell 2x2 ở mép dưới bản đồ). Biểu diễn bằng `List<Vector2Int> path`:
`A -> B -> C -> D -> A`

### Bước 2: Kéo giãn 1 cạnh thành hình chữ U (Extrusion / Notch Mutation)
- Chạy lặp `N` lần theo chỉ số `complexity`.
- Bốc ngẫu nhiên một cạnh bất kì nối giữa 2 điểm kề nhau trong `path` hiện tại, ví dụ: `A -> B`.
- Tính vector pháp tuyến (Normal) của cạnh này. Thử cả chiều trái lẫn chiều phải dọc theo pháp tuyến khoảng cách là 1 ô. Tạo ra 2 điểm mới: `C` và `D`.
- **Kiểm tra độ chèn lấn:** Nếu `C` và `D` không bị tràn viền (out bounds), không chèn lên phần tử đang có trong mảng `path`, và không lấn vào khu "Protected Boarding Area" (Khu cấm bến xe buýt), ta chấp thuận.
- **Biến đổi List:** Thế chỗ nối thẳng `A -> B`. Chèn C và D vào giữa để tạo đường vòng uốn khúc: **`A -> C -> D -> B`**.

> **📝 Chìa khóa vàng của thuật toán:** Bằng cách kéo dãn đường ra theo cạnh liền kề thông qua không gian lưới trống, ta **chắc chắn 100%** không bao giờ tạo kết nối đan chéo đè lên nhau (no crossing). Một vòng duy nhất giãn nở liên tục cho đến khi đủ chiều dài.

### Bước 3: Áp cấu trúc Path xuống hệ Grid bằng Type chuẩn xác (No Auto-Tile)
Thay vì ném xuống lưới cục `GenericRoad` vô tráng, vì thuật toán quản lý cấu trúc vòng ở định dạng List 1 Chiều, với mỗi một Node `curr` mang tọa độ Grid, ta dễ dàng truy vết điểm liền trước (`prev`) và điểm liền sau nó (`next`).
- Tính vector Đi vào (`dirIn = curr - prev`) và vector Đi ra (`dirOut = next - curr`).
- Nếu Đi vào đi thẳng 1 phương, chọn `Straight`.
- Phối 2 hướng vuông góc: Chọn `Corner` tương ứng một cách **CHÍNH XÁC**.
  *(Ví dụ: Từ Dưới đi lên (`dirIn = UP`), Rẽ Sang Phải (`dirOut = RIGHT`) => Góc bẻ L `Corner_SE`)*.
- Gán toàn bộ lưới. Không bao giờ chạy qua `UpdateAllRoadTypes` nữa.

## 3. Khả năng "Đánh lừa thị giác"
Các đoạn vòng cua uốn hẹp hình "cổ chai", "đồng hồ cát". Hai ô vuông nằm kẹp sát nhau trên lưới, nhưng thực chất ở định dạng mảng 1D, chúng ở điểm cực thứ 10 và thứ 30. Chúng thuộc 2 dòng logic hoàn toàn nối xa nhưng vô tình nằm cạnh nhau về mặt vật lý. Grid Game hoàn toàn chấp thuận vì ô này là `Corner_SE`, ô kia là `Corner_SW`, mesh của Asset tạo ra ảo ảnh dính vách 100% siêu trơn tru trên hình ảnh Render, tạo mật độ (Density) kịch kim mà không vi phạm luật chơi!
