import XCTest
@testable import ClaudeUsageCore

final class CookieAuthTests: XCTestCase {
    private func cookie(name: String, value: String, domain: String) -> HTTPCookie {
        HTTPCookie(properties: [
            .name: name, .value: value, .domain: domain, .path: "/",
        ])!
    }

    func test_findsSessionKeyForClaudeDomain() {
        let cookies = [
            cookie(name: "other", value: "x", domain: ".claude.ai"),
            cookie(name: "sessionKey", value: "sk-abc", domain: ".claude.ai"),
        ]
        XCTAssertEqual(sessionKeyValue(from: cookies), "sk-abc")
    }
    func test_nilWhenAbsent() {
        XCTAssertNil(sessionKeyValue(from: [cookie(name: "other", value: "x", domain: ".claude.ai")]))
    }
    func test_ignoresWrongDomain() {
        XCTAssertNil(sessionKeyValue(from: [cookie(name: "sessionKey", value: "sk", domain: ".example.com")]))
    }
    func test_emptyList() {
        XCTAssertNil(sessionKeyValue(from: []))
    }
}
