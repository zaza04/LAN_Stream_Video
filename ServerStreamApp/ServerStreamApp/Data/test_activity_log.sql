-- Script để kiểm tra ActivityLog table
-- Chạy script này trong SQLite để xem data

-- Xem structure của table
.schema ActivityLog

-- Xem tất cả records trong ActivityLog
SELECT * FROM ActivityLog ORDER BY Timestamp DESC;

-- Đếm số lượng activities theo type
SELECT ActivityType, COUNT(*) as Count 
FROM ActivityLog 
GROUP BY ActivityType 
ORDER BY Count DESC;

-- Xem activities của user cụ thể (thay 212 bằng userId khác)
SELECT al.*, u.Username 
FROM ActivityLog al
LEFT JOIN Users u ON al.UserId = u.UserId
WHERE al.UserId = 212
ORDER BY al.Timestamp DESC;

-- Xem activities trong 24h gần nhất
SELECT al.*, u.Username 
FROM ActivityLog al
LEFT JOIN Users u ON al.UserId = u.UserId
WHERE datetime(al.Timestamp) >= datetime('now', '-1 day')
ORDER BY al.Timestamp DESC;