# Nomad Adventure Дипломын Илтгэлийн Баримт Бичиг

## 1. Төслийн нэр

**Nomad Adventure: React Native, Unity, PostgreSQL дээр суурилсан аялал жуулчлалын AR интерактив систем**

## 2. Төслийн товч танилцуулга

Nomad Adventure нь Улаанбаатар хотын аялал жуулчлалын цэгүүдийг нэмэлт бодит орчин (AR) болон тоглоомчилсон хэрэглэгчийн туршлагатай хослуулан танилцуулах зорилготой гар утасны систем юм. Энэхүү систем нь хэрэглэгчийг газрын зураг дээрх онцлох байршлуудаар чиглүүлж, тухайн цэг дээр очсон үед Unity дээр суурилсан AR орчин руу шилжүүлэн, виртуал шагнал цуглуулах боломж олгодог.

Төслийн үндсэн санаа нь аялал жуулчлалын контентыг уламжлалт текстэн мэдээллээс илүү сонирхолтой, интерактив, оролцоонд суурилсан хэлбэрт шилжүүлэхэд оршино.

## 3. Асуудлын үндэслэл

Одоогийн аялал жуулчлалын танилцуулгын олон систем нь:

- Хэрэглэгчийн оролцоог бага татдаг
- Зөвхөн мэдээлэл харах түвшинд хязгаарлагддаг
- Байршилд суурилсан урамшуулал, интерактив элемент дутмаг
- Залуучуудад чиглэсэн тоглоомчилсон хэрэглээ хангалтгүй

Энэ асуудлыг шийдэхийн тулд байршил, AR дүрслэл, шагналын систем, хэрэглэгчийн профайл, олон хэлний дэмжлэгийг нэг системд нэгтгэсэн шийдэл боловсруулсан.

## 4. Төслийн зорилго

Энэхүү төслийн зорилго нь:

- Аялал жуулчлалын байршлуудыг газрын зураг дээр интерактив байдлаар харуулах
- Unity ашиглан AR туршлага үүсгэх
- Хэрэглэгчдэд байршил бүрээс виртуал шагнал авах боломж олгох
- Хэрэглэгчийн шагнал, профайл, хэлний тохиргоог удирдах
- Backend болон database-тэй уялдсан бүрэн ажиллагаатай систем байгуулах

## 5. Зорилтууд

Төслийн хүрээнд дараах зорилтуудыг тавьсан:

1. Expo React Native ашиглан мобайл интерфэйс боловсруулах
2. Unity framework-ийг мобайл апптай холбох
3. PostgreSQL болон Prisma ашиглан өгөгдлийн сангийн бүтэц боловсруулах
4. Байршлын цэг, шагнал, хэрэглэгчийн өгөгдлийг backend API-аар удирдах
5. Хэрэглэгчийн бүртгэл, email OTP баталгаажуулалтын боломж нэмэх
6. Монгол, Англи хэлний дэмжлэгтэй интерфэйс хийх
7. Системийн алдаа, архитектурын эрсдэлийг шинжилж сайжруулах

## 6. Ашигласан технологиуд

### Frontend

- React Native
- Expo
- Expo Router
- TypeScript
- Mapbox
- Expo Image
- Expo Splash Screen

### Backend

- Node.js
- Express.js
- Prisma ORM
- PostgreSQL
- Zod validation

### AR болон 3D интеграц

- Unity
- `@azesmway/react-native-unity`
- Custom Expo config plugin

### Нэмэлт боломжууд

- Олон хэлний дэмжлэг
- Email OTP баталгаажуулалт
- Тоглоомчилсон шагналын систем

## 7. Системийн ерөнхий архитектур

Систем нь 3 үндсэн давхаргатай.

### 7.1 Mobile Client

Mobile client нь React Native дээр ажиллах ба дараах үндсэн дэлгэцүүдтэй:

- Map screen
- Reward / Ledger screen
- Profile screen
- AR screen
- Guide / modal screen

### 7.2 Backend API

Backend нь REST API хэлбэрээр ажиллана. Үндсэн үүргүүд:

- Guest login
- Email OTP request / verify
- Tourism spot listing
- Reward claiming
- User reward history

### 7.3 Database Layer

Prisma ORM-аар PostgreSQL өгөгдлийн сантай холбогдоно.

Үндсэн entity-үүд:

- `User`
- `Reward`
- `TourismSpot`
- `UserReward`
- `EmailOtp`

## 8. Өгөгдлийн сангийн бүтэц

### User

Хэрэглэгчийн үндсэн мэдээлэл хадгална.

- `id`
- `deviceId`
- `displayName`
- `email`
- `emailVerifiedAt`
- `lastLoginAt`

### Reward

Шагналын мэдээлэл.

- `name`
- `description`
- `imageUrl`
- `previewPrefabKey`

### TourismSpot

Газрын зураг дээрх аялал жуулчлалын байршил.

- `name`
- `description`
- `latitude`
- `longitude`
- `radiusMeters`
- `rewardId`

### UserReward

Хэрэглэгчийн цуглуулсан шагналын мэдээлэл.

- `userId`
- `rewardId`
- `tourismSpotId`
- `claimedAt`

### EmailOtp

Имэйл баталгаажуулалтын OTP кодын мэдээлэл.

- `userId`
- `email`
- `codeHash`
- `expiresAt`
- `attempts`
- `consumedAt`

## 9. Системийн ажиллагааны урсгал

### 9.1 Нэвтрэх урсгал

1. Апп нээгдэхэд guest session үүсгэнэ
2. Backend хэрэглэгчийг `deviceId` ашиглан шинээр үүсгэх эсвэл сэргээнэ
3. JWT access token frontend-д буцна

### 9.2 Газрын зураг ашиглах урсгал

1. Backend-ээс идэвхтэй tourism spot-уудыг авна
2. Mapbox дээр marker хэлбэрээр дүрслэнэ
3. Marker эсвэл nearby drawer дээр дарж AR дэлгэц рүү орно

### 9.3 AR урсгал

1. Unity scene ачаална
2. React Native талаас spot мэдээллийг Unity рүү дамжуулна
3. Unity-ээс `ready` эсвэл `collected` message ирнэ
4. `collected` үед reward claim API дуудагдана

### 9.4 Шагналын урсгал

1. Хэрэглэгч тодорхой tourism spot дээр reward claim хийнэ
2. Backend duplicate claim-ийг шалгана
3. Шагнал `UserReward` хүснэгтэд хадгалагдана
4. Reward ledger дэлгэц шинэчлэгдэнэ

### 9.5 Email OTP урсгал

1. Хэрэглэгч email хаяг оруулна
2. Backend OTP код үүсгэнэ
3. Код Gmail-ээр илгээгдэнэ
4. Хэрэглэгч OTP оруулж баталгаажуулна
5. Email verified төлөв backend дээр хадгалагдана

## 10. Хэрэгжүүлсэн гол боломжууд

### 10.1 Map UI

- Mapbox ашигласан газрын зураг
- Custom marker дизайн
- Nearby route drawer
- Profile quick access

### 10.2 Reward Ledger

- Цуглуулсан болон цоожтой шагналын картууд
- Reward history
- Localized UI

### 10.3 Profile хэсэг

- Хэрэглэгчийн товч мэдээлэл
- Хэл солих
- Route board modal
- Guide screen
- Email verification

### 10.4 Unity Integration

- Unity view дуудах
- React Native → Unity message
- Unity → React Native message
- Mock Unity mode ашиглан simulator дээр UI хөгжүүлэх боломж

## 11. Кодын шалгалтаар илэрсэн гол асуудлууд

Энэ хэсэг нь системийн кодын чанарын шинжилгээнд суурилсан.

### 11.1 Guest хэрэглэгчийн ID тогтвортой биш байх эрсдэл

Frontend дээр хэрэглэгчийн guest identity-г `Constants.sessionId` дээр тулгуурлан үүсгэж байна. Энэ утга нь session бүрт өөрчлөгдөх боломжтой тул апп дахин нээгдэхэд шинэ guest user үүсэх эрсдэлтэй.

**Нөлөө:**

- Хуучин шагналууд алдагдах
- Нэг хэрэглэгч олон account үүсгэх
- Reward history тасрах

**Шийдэл:**

- Device identifier-ийг SecureStore эсвэл local persistent storage-д хадгалах

### 11.2 Reward claim хийх үед байршлын шалгалт байхгүй

Backend дээр `radiusMeters`, `latitude`, `longitude` өгөгдөл хадгалагдаж байгаа боловч reward claim хийх үед хэрэглэгч тухайн цэг дээр байгаа эсэхийг баталгаажуулах логик дутуу байна.

**Нөлөө:**

- Хэрэглэгч дурын reward-ийг хаанаас ч авах боломжтой
- Системийн тоглоомчилсон утга алдагдана

**Шийдэл:**

- Claim API дээр client location авч
- Backend дээр distance тооцоолж
- Radius-оос хэтэрвэл reward claim-ийг хориглох

### 11.3 TypeScript model mismatch

Home map screen дээр `spot.imageUrl` ашиглаж байгаа боловч `Spot` type дээр ийм property байхгүй. Backend serializer ч энэ талбарыг буцаахгүй байна.

**Нөлөө:**

- TypeScript build алдаа
- Runtime fallback логик буруу ажиллах магадлал

**Шийдэл:**

- `spot.reward?.imageUrl` ашиглах
эсвэл
- API schema-г нэг мөр болгох

### 11.4 Unity loading state дээр edge case байгаа

Unity screen дээр timeout-аас болж `loadError = true` болсон үед Unity дараа нь `ready` signal өгсөн ч error state бүрэн арилахгүй байж болно.

**Нөлөө:**

- Unity scene ачаалсан ч хэрэглэгч error дэлгэц дээр үлдэнэ

**Шийдэл:**

- `markReady()` дотор `loadError`-ийг заавал reset хийх
- Давхар timeout болон postMessage timer-уудыг удирдах

### 11.5 Email OTP flow cross-device login-ийг бүрэн шийдээгүй

Одоогийн бүтэц нь guest user-ийг email verified болгох чиглэлтэй боловч өөр төхөөрөмжөөс email-ээр нэвтрэх бүрэн auth flow хараахан байхгүй.

**Нөлөө:**

- Email verification нь зөвхөн “энэ төхөөрөмжийн account”-ийг баталгаажуулж байна
- Cross-device хэрэглээ бүрэн дэмжигдээгүй

**Шийдэл:**

- Email login / passwordless login endpoint нэмэх
- Device болон user-г тусад нь model болгох

### 11.6 OTP request ба verify урсгал дээр race condition байж болно

OTP request хийх, өмнөх кодыг хүчингүй болгох, шинэ код үүсгэх нь тусдаа query-ээр хийгдэж байгаа тул зэрэгцээ хүсэлтүүдийн үед олон идэвхтэй OTP үлдэх эрсдэлтэй.

**Нөлөө:**

- Нэгээс олон идэвхтэй код үүсэх
- OTP баталгаажуулалт тогтворгүй болох

**Шийдэл:**

- Prisma transaction ашиглах
- `(userId, purpose)` дээр unique constraint нэмэх

### 11.7 Unity config plugin fragile бүтэцтэй

Custom `withUnity.js` plugin нь Podfile болон Gradle файлуудыг string replace аргаар өөрчилж байна.

**Нөлөө:**

- Expo prebuild дараа дахин эвдрэх
- Podfile формат өөрчлөгдвөл plugin ажиллахгүй болох
- Unity framework path mismatch гарах магадлалтай

**Шийдэл:**

- Илүү тогтвортой config-plugin API ашиглах
- Build artifact path-уудыг баталгаатай стандартчлах

## 12. Сайжруулалтын санал

Цаашид дараах сайжруулалтуудыг хийх боломжтой.

### 12.1 Authentication

- Stable device identity
- Email-based passwordless login
- Refresh token support
- Session persistence

### 12.2 Gameplay Logic

- Geofence шалгалт
- Reward cooldown
- Anti-spoof validation

### 12.3 Unity Integration

- Scene readiness state machine
- Native bridge protocol standardization
- Mock/real mode separation

### 12.4 Frontend UX

- Marker clustering
- Camera focus animation
- Better offline state
- Cached reward assets

### 12.5 Backend Quality

- Better transaction safety
- Structured logging
- Rate limiting
- OTP abuse protection

## 13. Төслийн давуу тал

Энэхүү төслийн давуу талууд:

- Мобайл UI, backend, database, Unity AR гэсэн олон технологийг нэгтгэсэн
- Аялал жуулчлалыг тоглоомчилсон байдлаар шийдсэн
- Олон хэлний дэмжлэгтэй
- Хэрэглэгчийн reward system-тэй
- AR ашигласан тул энгийн мэдээллийн системээс илүү интерактив

## 14. Төслийн шинэлэг тал

Төслийн шинэлэг тал нь:

- Улаанбаатарын аялал жуулчлалын цэгүүдийг AR ба reward system-тэй хослуулсан
- React Native ба Unity framework-ийг нэг мобайл системд уялдуулсан
- Аялал жуулчлалын мэдээллийг тоглоомчилсон интерфэйсээр хүргэж байгаа

## 15. Туршилт ба үнэлгээ

Систем дээр дараах туршилтууд хийгдсэн:

- API endpoint ачаалал ба response шалгалт
- Prisma schema validation
- Frontend lint шалгалт
- Unity mock mode дээр simulator хөгжүүлэлт
- Reward claim flow шалгалт
- Email OTP flow-ийн үндсэн тест

Дипломын live demo-д зориулж дараах туршилтын сценари бэлтгэсэн:

- `ssodko245@gmail.com` тест хэрэглэгч үүсгэж, demo login-аар шууд нэвтрэх боломж бүрдүүлсэн
- Нийт 6 tourism spot seed хийж, тэдгээрийн 3 дээр reward claim хийгдсэн төлөв үүсгэсэн
- `Sukhbaatar Square` байршил дээр reward-ийг claim хийгдээгүй төлөвт үлдээж, physical phone дээр газар дээр нь очоод харагдац болон claim validation-ийг шалгах нөхцөл бүрдүүлсэн
- Mobile app-ийн API хаягийг дотоод сүлжээний IP руу тохируулж, simulator биш бодит төхөөрөмжөөс backend-т холбогдох боломжийг бэлдсэн

Цаашид хийх шаардлагатай туршилтууд:

- Physical device дээр Unity production test
- GPS accuracy test
- Email delivery reliability test
- Network interruption recovery test

## 16. Дүгнэлт

Nomad Adventure төсөл нь аялал жуулчлалын мэдээллийн системийг AR, байршилд суурилсан reward mechanism, мобайл UX, backend architecture-тай хослуулсан цогц шийдэл болж чадсан. Төслийн хүрээнд frontend, backend, database, Unity integration гэсэн олон түвшний хөгжүүлэлт хийгдсэн нь практик болон судалгааны өндөр ач холбогдолтой.

Мөн кодын чанарын үнэлгээгээр илэрсэн асуудлууд нь системийг цаашид production түвшинд боловсронгуй болгох тодорхой чиглэлийг гаргаж өгсөн. Ялангуяа authentication, geofence logic, Unity bridge stability, OTP transaction safety зэрэг нь дараагийн хөгжүүлэлтийн гол чиглэл болно.

Иймд энэхүү төсөл нь аялал жуулчлалын дижитал шийдэл, тоглоомчилол, нэмэлт бодит орчныг хослуулсан ирээдүйтэй платформын суурь болсон гэж дүгнэж байна.

## 17. Илтгэл дээр ашиглах товч слайд бүтэц

Хэрэв слайд болгох бол дараах бүтэц тохиромжтой.

1. Төслийн нэр ба зорилго
2. Асуудлын үндэслэл
3. Системийн ерөнхий шийдэл
4. Ашигласан технологиуд
5. Архитектурын схем
6. Database бүтэц
7. Map + AR + Reward урсгал
8. Email OTP ба профайл удирдлага
9. Илэрсэн техникийн асуудлууд
10. Сайжруулалтын санал
11. Дүгнэлт
12. Асуулт, хариулт

## 18. Хамгаалалтын үеэр хэлж болох товч аман танилцуулга

Сайн байцгаана уу. Миний дипломын ажлын сэдэв бол Nomad Adventure нэртэй аялал жуулчлалын AR интерактив систем юм. Энэхүү төслийн зорилго нь Улаанбаатар хотын онцлох байршлуудыг газрын зураг, Unity дээр суурилсан AR туршлага, мөн тоглоомчилсон шагналын системтэйгээр нэгтгэн харуулахад оршино. Төслийг React Native Expo, Node.js, PostgreSQL, Prisma, Unity технологиуд дээр хэрэгжүүлсэн.

Системийн хүрээнд хэрэглэгч газрын зураг дээрх цэгүүдийг харж, тухайн байршил руу орж AR орчинд шагнал цуглуулах боломжтой. Мөн хэрэглэгчийн reward history, profile, хэлний тохиргоо, email OTP баталгаажуулалтын боломжуудыг боловсруулсан.

Кодын түвшний үнэлгээгээр guest user identity, geofence шалгалт, Unity loading state, OTP race condition зэрэг хэд хэдэн чухал сайжруулалтын асуудлууд илэрсэн бөгөөд эдгээрийг дараагийн хөгжүүлэлтийн чиглэл болгон тодорхойлсон.

Энэхүү төсөл нь аялал жуулчлалын салбарт интерактив, тоглоомчилсон, орчин үеийн дижитал шийдэл бий болгох боломжтойг харуулж байна.



docker run --name nomad-postgres \
  -e POSTGRES_USER=myuser \
  -e POSTGRES_PASSWORD=mypassword \
  -e POSTGRES_DB=nomad_adventure \
  -p 5432:5432 \
  -d postgis/postgis:15-3.4
