import Foundation

/// Return the value of the `sessionKey` cookie for a claude.ai domain, if present.
public func sessionKeyValue(from cookies: [HTTPCookie]) -> String? {
    cookies.first { $0.name == "sessionKey" && $0.domain.contains("claude.ai") }?.value
}
