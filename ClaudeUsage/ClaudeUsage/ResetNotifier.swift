import UserNotifications

/// Local notification for the 5-hour limit reset.
enum ResetNotifier {
    /// Ask for permission (called when the user enables the toggle).
    static func requestAuthorization() {
        UNUserNotificationCenter.current()
            .requestAuthorization(options: [.alert, .sound]) { _, _ in }
    }

    /// Post the reset notification. A stable identifier means a new one replaces
    /// any prior (resets are ~5h apart, so this never stacks up).
    static func notifyReset() {
        let content = UNMutableNotificationContent()
        content.title = "Claude limit reset"
        content.body = "Your 5-hour session limit has reset."
        let request = UNNotificationRequest(
            identifier: "claude-limit-reset", content: content, trigger: nil)
        UNUserNotificationCenter.current().add(request)
    }
}
