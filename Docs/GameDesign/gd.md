Cơ chế cản đường & Queue (Tile-based Reservation):

Em đã bổ sung logic "Grid Occupation (Chiếm ô)" vào hệ thống Pathing của GameManager. Các xe giờ đây không thể đi xuyên qua nhau nữa!
Quy tắc đưa ra là: Mỗi xe sẽ đánh dấu "chiếm" một ô vuông (targetGridPos) ngay khi bắt đầu rướn vào, hoặc ô hiện tại nếu nó đang lùi lại/đứng chờ (currentGridPos).
Khi một xe đến lượt gọi hàm MoveAlongPath để tiến tới ô tiếp theo (ví dụ ô ngã ba lấy khách), nếu phát hiện ô kế tiếp đã có xe khác chiếm (hoặc xe khác đang rướn tới đó), nó tự động huỷ bỏ di chuyển, đứng yên và bị đưa vào danh sách waitingToMoveBuses.
Hệ thống sẽ liên tục rà soát ProcessWaitingBuses() trong Update(), ngay khi xe phía trước nhích đi, ô trống lộ ra, các xe đang bị dồn ứ (nằm chờ) sẽ tự động theo thứ tự lần lượt đi tiếp. Điều này tạo hiệu ứng xe buýt bị kẹt và nối đuôi cách nhau đúng 1 block khoảng cách Offset cực kỳ tự nhiên y như Bus Jam!
Cơ chế Roaming vòng lặp & lấp đầy Capacity:

Logic Check Full của xe buýt đã được thiết lập nghiêm ngặt: bus.currentPassengerCount >= capacity (với capacity là 32 như bác quy định).
Khi Load người (LoadPassengersCoroutine), xe chỉ nhận vừa đủ người tuỳ thao tác tap của bác. Ví dụ bác tap vào nhóm 10 người, xe chỉ cộng dồn 10 điểm sức chứa. Lúc này isFull = false, xe tự động Tiếp tục tham gia vào dòng Loop hoà cùng các xe khác thay vì đi về bãi đáp gốc.
Cứ lượn như vậy cho đến kì gọi nào đó, đủ 32/32 người thì isFull bằng True. Từ đây xe mới tự động rẽ nhánh tìm về bến dừng (Bus Stop) mẹ đẻ ban đầu và xoá sổ nó!
Code đã được lưu thành công! Bác có thể bật màn Play lên test ngay xem đã đúng chuẩn "nghẽn mạch giao thông nối đuôi" chưa nhen!

nên nhớ mặc dù các nhóm có thể có số agetns từ [8, 40] nhưng khi cộng tổng tất cả các nhóm trong các land thì số agents tương ứng với mỗi màu vẫn phải đủ bằng số (bus stop length) x (buses per stop) x (agents per bus) nhé