import XCTest
@testable import ClaudeUsageCore

final class ResetDetectionTests: XCTestCase {
    func test_firstFetchIsNeverReset() {
        XCTAssertFalse(resetOccurred(previous: nil, current: 18000))
    }
    func test_countdownIsNotReset() {
        XCTAssertFalse(resetOccurred(previous: 1200, current: 1199))
    }
    func test_smallJitterIsNotReset() {
        XCTAssertFalse(resetOccurred(previous: 1200, current: 1210)) // within 60s margin
    }
    func test_upwardJumpIsReset() {
        XCTAssertTrue(resetOccurred(previous: 30, current: 18000))
    }
    func test_fromZeroToNewWindowIsReset() {
        XCTAssertTrue(resetOccurred(previous: 0, current: 17999))
    }
}
