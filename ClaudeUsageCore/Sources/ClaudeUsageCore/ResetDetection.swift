import Foundation

/// True when the 5-hour window rolled over. `secondsToReset` counts down within
/// a window and jumps up on reset; the first fetch (`previous == nil`) is never a
/// reset. The 60s margin absorbs poll jitter (a real reset jumps by thousands).
public func resetOccurred(previous: Int?, current: Int) -> Bool {
    guard let previous else { return false }
    return current > previous + 60
}
