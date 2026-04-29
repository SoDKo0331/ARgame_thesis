import AppKit
import CoreText
import Foundation

struct Palette {
  static let ink = NSColor(hex: "#303841")
  static let surge = NSColor(hex: "#00ADB5")
  static let paper = NSColor(hex: "#EEEEEE")
  static let ember = NSColor(hex: "#FF5722")
}

extension NSColor {
  convenience init(hex: String, alpha: CGFloat = 1) {
    let cleaned = hex.replacingOccurrences(of: "#", with: "")
    let scanner = Scanner(string: cleaned)
    var value: UInt64 = 0
    scanner.scanHexInt64(&value)

    let red = CGFloat((value >> 16) & 0xFF) / 255
    let green = CGFloat((value >> 8) & 0xFF) / 255
    let blue = CGFloat(value & 0xFF) / 255

    self.init(red: red, green: green, blue: blue, alpha: alpha)
  }
}

func registerFont(at url: URL) {
  var error: Unmanaged<CFError>?
  CTFontManagerRegisterFontsForURL(url as CFURL, .process, &error)
}

func fontName(from url: URL) -> String? {
  guard
    let descriptors = CTFontManagerCreateFontDescriptorsFromURL(url as CFURL) as? [CTFontDescriptor],
    let descriptor = descriptors.first,
    let name = CTFontDescriptorCopyAttribute(descriptor, kCTFontNameAttribute) as? String
  else {
    return nil
  }

  return name
}

func pngCanvas(
  width: Int,
  height: Int,
  opaque: Bool = false,
  draw: (CGContext, CGRect) -> Void
) -> Data {
  let rep = NSBitmapImageRep(
    bitmapDataPlanes: nil,
    pixelsWide: width,
    pixelsHigh: height,
    bitsPerSample: 8,
    samplesPerPixel: 4,
    hasAlpha: true,
    isPlanar: false,
    colorSpaceName: .deviceRGB,
    bytesPerRow: 0,
    bitsPerPixel: 0
  )!

  let context = NSGraphicsContext(bitmapImageRep: rep)!
  NSGraphicsContext.saveGraphicsState()
  NSGraphicsContext.current = context

  let cg = context.cgContext
  cg.setAllowsAntialiasing(true)
  cg.interpolationQuality = .high
  draw(cg, CGRect(x: 0, y: 0, width: width, height: height))

  NSGraphicsContext.restoreGraphicsState()
  return rep.representation(using: .png, properties: [:])!
}

func withSavedState(_ cg: CGContext, _ draw: () -> Void) {
  cg.saveGState()
  draw()
  cg.restoreGState()
}

func deg2rad(_ degrees: CGFloat) -> CGFloat {
  degrees * .pi / 180
}

func roundedRectPath(in rect: CGRect, radius: CGFloat) -> CGPath {
  CGPath(roundedRect: rect, cornerWidth: radius, cornerHeight: radius, transform: nil)
}

func pinPath(in rect: CGRect) -> CGPath {
  let path = CGMutablePath()
  let midX = rect.midX
  let minX = rect.minX
  let maxX = rect.maxX
  let minY = rect.minY
  let maxY = rect.maxY

  path.move(to: CGPoint(x: midX, y: minY))
  path.addCurve(
    to: CGPoint(x: minX + rect.width * 0.12, y: minY + rect.height * 0.36),
    control1: CGPoint(x: midX - rect.width * 0.24, y: minY + rect.height * 0.06),
    control2: CGPoint(x: minX + rect.width * 0.08, y: minY + rect.height * 0.18)
  )
  path.addCurve(
    to: CGPoint(x: midX, y: maxY),
    control1: CGPoint(x: minX + rect.width * 0.02, y: minY + rect.height * 0.76),
    control2: CGPoint(x: minX + rect.width * 0.26, y: maxY)
  )
  path.addCurve(
    to: CGPoint(x: maxX - rect.width * 0.12, y: minY + rect.height * 0.36),
    control1: CGPoint(x: maxX - rect.width * 0.26, y: maxY),
    control2: CGPoint(x: maxX - rect.width * 0.02, y: minY + rect.height * 0.76)
  )
  path.addCurve(
    to: CGPoint(x: midX, y: minY),
    control1: CGPoint(x: maxX - rect.width * 0.08, y: minY + rect.height * 0.18),
    control2: CGPoint(x: midX + rect.width * 0.24, y: minY + rect.height * 0.06)
  )
  path.closeSubpath()

  return path
}

func strokePath(_ cg: CGContext, path: CGPath, color: NSColor, width: CGFloat) {
  cg.addPath(path)
  cg.setStrokeColor(color.cgColor)
  cg.setLineWidth(width)
  cg.setLineJoin(.round)
  cg.setLineCap(.round)
  cg.strokePath()
}

func fillPath(_ cg: CGContext, path: CGPath, color: NSColor) {
  cg.addPath(path)
  cg.setFillColor(color.cgColor)
  cg.fillPath()
}

func fillRoundedRect(_ cg: CGContext, rect: CGRect, radius: CGFloat, color: NSColor) {
  fillPath(cg, path: roundedRectPath(in: rect, radius: radius), color: color)
}

func strokeRoundedRect(_ cg: CGContext, rect: CGRect, radius: CGFloat, color: NSColor, width: CGFloat) {
  strokePath(cg, path: roundedRectPath(in: rect, radius: radius), color: color, width: width)
}

func drawRouteLine(
  _ cg: CGContext,
  circleRect: CGRect,
  routeColor: NSColor,
  nodeFill: NSColor,
  nodeStroke: NSColor
) {
  let p1 = CGPoint(x: circleRect.minX + circleRect.width * 0.22, y: circleRect.minY + circleRect.height * 0.78)
  let p2 = CGPoint(x: circleRect.minX + circleRect.width * 0.22, y: circleRect.minY + circleRect.height * 0.22)
  let p3 = CGPoint(x: circleRect.minX + circleRect.width * 0.68, y: circleRect.minY + circleRect.height * 0.74)
  let p4 = CGPoint(x: circleRect.minX + circleRect.width * 0.68, y: circleRect.minY + circleRect.height * 0.26)

  let route = CGMutablePath()
  route.move(to: p1)
  route.addLine(to: p2)
  route.addLine(to: p3)
  route.addLine(to: p4)

  cg.addPath(route)
  cg.setStrokeColor(routeColor.cgColor)
  cg.setLineWidth(circleRect.width * 0.12)
  cg.setLineCap(.round)
  cg.setLineJoin(.round)
  cg.strokePath()

  let nodeRadius = circleRect.width * 0.092
  for point in [p1, p3, p4] {
    let nodeRect = CGRect(
      x: point.x - nodeRadius,
      y: point.y - nodeRadius,
      width: nodeRadius * 2,
      height: nodeRadius * 2
    )
    fillPath(cg, path: CGPath(ellipseIn: nodeRect, transform: nil), color: nodeFill)
    strokePath(cg, path: CGPath(ellipseIn: nodeRect, transform: nil), color: nodeStroke, width: circleRect.width * 0.045)
  }
}

func drawBrandMark(
  _ cg: CGContext,
  in rect: CGRect,
  showCard: Bool,
  showBaseBackground: Bool,
  monochrome: Bool = false
) {
  let baseInset = rect.width * 0.06
  let baseRect = rect.insetBy(dx: baseInset, dy: baseInset)

  if showBaseBackground {
    fillRoundedRect(cg, rect: baseRect, radius: rect.width * 0.18, color: Palette.ink)
  }

  let cardSize = rect.width * (showBaseBackground ? 0.63 : 0.72)
  let cardRect = CGRect(
    x: rect.midX - cardSize / 2,
    y: rect.midY - cardSize / 2 + rect.height * 0.02,
    width: cardSize,
    height: cardSize
  )

  if showCard {
    withSavedState(cg) {
      cg.translateBy(x: cardRect.midX, y: cardRect.midY)
      cg.rotate(by: deg2rad(-8))

      let localRect = CGRect(x: -cardRect.width / 2, y: -cardRect.height / 2, width: cardRect.width, height: cardRect.height)
      let shadowRect = localRect.offsetBy(dx: cardRect.width * 0.05, dy: -cardRect.height * 0.05)

      fillPath(cg, path: roundedRectPath(in: shadowRect, radius: cardRect.width * 0.1), color: monochrome ? NSColor.black : Palette.ink)
      fillPath(cg, path: roundedRectPath(in: localRect, radius: cardRect.width * 0.1), color: monochrome ? NSColor.black : Palette.paper)
      if !monochrome {
        strokePath(cg, path: roundedRectPath(in: localRect, radius: cardRect.width * 0.1), color: Palette.ink, width: cardRect.width * 0.045)
      }
    }
  }

  let pinRect = CGRect(
    x: rect.midX - rect.width * 0.19,
    y: rect.midY - rect.height * 0.18,
    width: rect.width * 0.38,
    height: rect.height * 0.5
  )

  let pin = pinPath(in: pinRect)
  fillPath(cg, path: pin, color: monochrome ? NSColor.black : Palette.surge)
  if !monochrome {
    strokePath(cg, path: pin, color: Palette.ink, width: rect.width * 0.035)
  }

  let circleRect = CGRect(
    x: pinRect.minX + pinRect.width * 0.22,
    y: pinRect.minY + pinRect.height * 0.34,
    width: pinRect.width * 0.56,
    height: pinRect.width * 0.56
  )

  if monochrome {
    withSavedState(cg) {
      cg.setBlendMode(.clear)
      cg.fillEllipse(in: circleRect)
    }
    drawRouteLine(cg, circleRect: circleRect, routeColor: .black, nodeFill: .black, nodeStroke: .clear)
  } else {
    fillPath(cg, path: CGPath(ellipseIn: circleRect, transform: nil), color: Palette.paper)
    strokePath(cg, path: CGPath(ellipseIn: circleRect, transform: nil), color: Palette.ink, width: rect.width * 0.03)
    drawRouteLine(cg, circleRect: circleRect, routeColor: Palette.ember, nodeFill: Palette.paper, nodeStroke: Palette.ink)
  }
}

func drawCenteredText(
  _ text: String,
  in rect: CGRect,
  font: NSFont,
  fill: NSColor,
  stroke: NSColor? = nil,
  strokeWidth: CGFloat = 0,
  shadowColor: NSColor? = nil,
  shadowOffset: CGSize = .zero
) {
  let paragraph = NSMutableParagraphStyle()
  paragraph.alignment = .center

  let attributes: [NSAttributedString.Key: Any] = [
    .font: font,
    .foregroundColor: fill,
    .paragraphStyle: paragraph,
  ]

  if let shadowColor {
    let shadowAttributes: [NSAttributedString.Key: Any] = [
      .font: font,
      .foregroundColor: shadowColor,
      .paragraphStyle: paragraph,
    ]
    let shadowText = NSAttributedString(string: text, attributes: shadowAttributes)
    shadowText.draw(in: rect.offsetBy(dx: shadowOffset.width, dy: shadowOffset.height))
  }

  if let stroke {
    let stroked: [NSAttributedString.Key: Any] = [
      .font: font,
      .foregroundColor: fill,
      .strokeColor: stroke,
      .strokeWidth: -strokeWidth,
      .paragraphStyle: paragraph,
    ]
    NSAttributedString(string: text, attributes: stroked).draw(in: rect)
  } else {
    NSAttributedString(string: text, attributes: attributes).draw(in: rect)
  }
}

let root = URL(fileURLWithPath: FileManager.default.currentDirectoryPath)
let assets = root.appendingPathComponent("assets/images")
let caveatBoldURL = root.appendingPathComponent("node_modules/@expo-google-fonts/caveat/700Bold/Caveat_700Bold.ttf")

registerFont(at: caveatBoldURL)
let caveatBoldName = fontName(from: caveatBoldURL) ?? "Caveat-Bold"
let caveat = NSFont(name: caveatBoldName, size: 1) ?? NSFont.systemFont(ofSize: 1, weight: .bold)
let avenir = NSFont(name: "AvenirNext-Bold", size: 1) ?? NSFont.systemFont(ofSize: 1, weight: .heavy)

let iconData = pngCanvas(width: 1024, height: 1024, opaque: true) { cg, rect in
  cg.setFillColor(Palette.ink.cgColor)
  cg.fill(rect)

  let stripeA = CGRect(x: rect.maxX - 250, y: rect.maxY - 440, width: 260, height: 620)
  let stripeB = CGRect(x: -40, y: 80, width: 320, height: 140)

  withSavedState(cg) {
    cg.translateBy(x: stripeA.midX, y: stripeA.midY)
    cg.rotate(by: deg2rad(9))
    fillRoundedRect(cg, rect: CGRect(x: -stripeA.width / 2, y: -stripeA.height / 2, width: stripeA.width, height: stripeA.height), radius: 48, color: Palette.surge)
  }

  withSavedState(cg) {
    cg.translateBy(x: stripeB.midX, y: stripeB.midY)
    cg.rotate(by: deg2rad(-8))
    fillRoundedRect(cg, rect: CGRect(x: -stripeB.width / 2, y: -stripeB.height / 2, width: stripeB.width, height: stripeB.height), radius: 36, color: Palette.ember)
  }

  drawBrandMark(cg, in: rect, showCard: true, showBaseBackground: false)
}

let foregroundData = pngCanvas(width: 1024, height: 1024) { cg, rect in
  drawBrandMark(cg, in: rect, showCard: true, showBaseBackground: false)
}

let monochromeData = pngCanvas(width: 1024, height: 1024) { cg, rect in
  drawBrandMark(cg, in: rect, showCard: false, showBaseBackground: false, monochrome: true)
}

let backgroundData = pngCanvas(width: 1024, height: 1024, opaque: true) { cg, rect in
  cg.setFillColor(Palette.ink.cgColor)
  cg.fill(rect)

  withSavedState(cg) {
    let stripe = CGRect(x: rect.maxX - 260, y: rect.midY - 140, width: 230, height: 720)
    cg.translateBy(x: stripe.midX, y: stripe.midY)
    cg.rotate(by: deg2rad(8))
    fillRoundedRect(cg, rect: CGRect(x: -stripe.width / 2, y: -stripe.height / 2, width: stripe.width, height: stripe.height), radius: 44, color: Palette.surge)
  }

  withSavedState(cg) {
    let stripe = CGRect(x: rect.midX - 310, y: 96, width: 420, height: 130)
    cg.translateBy(x: stripe.midX, y: stripe.midY)
    cg.rotate(by: deg2rad(-7))
    fillRoundedRect(cg, rect: CGRect(x: -stripe.width / 2, y: -stripe.height / 2, width: stripe.width, height: stripe.height), radius: 36, color: Palette.ember)
  }

  let ringRect = CGRect(x: rect.midX - 230, y: rect.midY - 230, width: 460, height: 460)
  strokePath(cg, path: CGPath(ellipseIn: ringRect, transform: nil), color: Palette.paper.withAlphaComponent(0.28), width: 32)
}

let faviconData = pngCanvas(width: 192, height: 192, opaque: true) { cg, rect in
  cg.setFillColor(Palette.ink.cgColor)
  cg.fill(rect)
  drawBrandMark(cg, in: rect, showCard: true, showBaseBackground: false)
}

let splashData = pngCanvas(width: 1200, height: 1200) { cg, rect in
  let markRect = CGRect(x: 250, y: 520, width: 700, height: 520)
  drawBrandMark(cg, in: markRect, showCard: true, showBaseBackground: false)

  let nomadFont = caveat.withSize(214)
  drawCenteredText(
    "Nomad",
    in: CGRect(x: 120, y: 248, width: 960, height: 240),
    font: nomadFont,
    fill: Palette.paper,
    stroke: Palette.ink,
    strokeWidth: 8,
    shadowColor: Palette.ember,
    shadowOffset: CGSize(width: 10, height: -10)
  )

  let pillRect = CGRect(x: 380, y: 154, width: 440, height: 94)
  fillRoundedRect(cg, rect: pillRect.offsetBy(dx: 14, dy: -14), radius: 44, color: Palette.ink)
  fillRoundedRect(cg, rect: pillRect, radius: 44, color: Palette.surge)
  strokeRoundedRect(cg, rect: pillRect, radius: 44, color: Palette.ink, width: 12)

  drawCenteredText(
    "AR ROUTES",
    in: pillRect.offsetBy(dx: 0, dy: 18),
    font: avenir.withSize(48),
    fill: Palette.paper
  )
}

let outputs: [(String, Data)] = [
  ("icon.png", iconData),
  ("android-icon-foreground.png", foregroundData),
  ("android-icon-monochrome.png", monochromeData),
  ("android-icon-background.png", backgroundData),
  ("favicon.png", faviconData),
  ("splash-icon.png", splashData),
]

for (name, data) in outputs {
  let url = assets.appendingPathComponent(name)
  try data.write(to: url)
  print("Wrote \(url.path)")
}
