Workspace System – Design Summary
TỔNG QUAN
•	- Hệ thống xoay quanh khái niệm workspace
•	- Mỗi workspace đại diện cho một ngữ cảnh làm việc
•	- Workspace không sở hữu dữ liệu, chỉ định nghĩa cấu trúc và cách hiển thị
•	- Không cho phép reference file/note/folder cross-owner giữa các workspace

CÁC THỰC THỂ CHÍNH
•	- Workspace
  - Là thực thể gốc
  - Có owner (user tạo workspace)
•	- Workspace không bao giờ chứa file của owner khác
  - Chỉ Owner hoặc user được cấp quyền mới CRUD workspace và bên trong nó
  - Mọi file/note/folder trong workspace luôn có owner = owner của workspace
  - khi workspace_item link đến folder/note/file mà deleteAt = x, thì workspace_item đó invalide, ta k hiển thị chúng

•	- Workspace_item
  - Định nghĩa cấu trúc hiển thị trong workspace (folder ảo, cây thư mục, thứ tự)
  - Chỉ trỏ một chiều tới file/note
  - Không sở hữu dữ liệu

•	- File / Note /Folder
  - Là thực thể độc lập, chứa nội dung
  - Có owner và creator (creator có thể khác owner)


COPY
•	- Không cho phép reference file/folder của workspace A vào workspace B
•	- Khi cần dùng lại dữ liệu từ user khác:
  - Thực hiện copy file/folder sang workspace mới hoặc tạo link
  - File copy có owner là user/workspace nhận copy
  - Có thể copy vào 1 workspace cụ thể, hoặc nếu là 1 file/note thì copy vào table file/note tương ứng
  - Dữ liệu độc lập, không sync
  - Lưu metadata trỏ về file gốc để phục vụ compare/sync trong tương lai

QUYỀN THAO TÁC
•	- Quyền thao tác dựa trên workspace permission
    - owner có mọi quyền
•	- User có quyền trong workspace thì có quyền tương ứng với mọi file/folder trong workspace đó
•	- Không tồn tại trường hợp file owner khác workspace owner

DELETE
•	- Khi delete không ở workspace (mà ở NoteGrid, FolderGrid, FileGrid, WsGrid), file/folder/note sẽ có deleteAt= x
•	- Khi delete ở workspace, workspace_item sẽ có deleteAt= x, khi đó toàn bộ con cháu bị "ẩn" khỏi workspace (xoá workspace_item sẽ không ảnh hưởng đến file/note gốc mà nó liên kết)
•	- User có quyền delete đều delete vào Bin của owner
•	- Owner luôn có thể restore


BIN
  - Chỉ owner thực hiện restore/permanent delete
  - khi permanent delete file/note/folder trong Bin, các workspace_items tương ứng cũng bị xoá theo, và đổi với folder thì các folder con cháu và workspace_item tương ứng cũng bị xoá luôn
  - Có option xoá luôn file/note con cháu (tính năng nâng cao)




