# 🚀 ParseAI

[![Lisans: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

ParseAI, PDF belgelerinizi yükleyip analiz etmenizi ve Gemini Yapay Zeka modeli sayesinde bu belgelerle akıllı bir şekilde sohbet etmenizi sağlayan tam yığın (full-stack) bir uygulamadır.

## 📑 İçindekiler

- [Özellikler](#-özellikler)
- [Teknolojiler](#-teknolojiler)
- [Kurulum](#-kurulum)
- [Kullanım](#-kullanım)
- [Katkıda Bulunma](#-katkıda-bulunma)
- [Lisans](#-lisans)
- [İletişim](#-iletişim)

## ✨ Özellikler

Projenin en önemli özellikleri:
- ✔️ **Yapay Zeka Destekli Belge Analizi**: Yüklediğiniz PDF dosyalarından anlamlı bilgiler çıkarır ve Google Gemini AI kullanarak bu verilerle etkileşime girmenizi sağlar.
- ✔️ **Akıllı Metin Parçalama (Smart Chunking)**: Büyük belgeleri otomatik olarak anlamlı küçük parçalara bölerek vektör veritabanında saklar.
- ✔️ **Gerçek Zamanlı Akış (Streaming)**: Yapay zeka yanıtlarını beklemeden kelime kelime, gerçek zamanlı olarak ekranda görürsünüz.
- ✔️ **Kullanıcı Dostu Arayüz**: Modern ve hızlı bir React tabanlı frontend ile kesintisiz bir kullanıcı deneyimi sunar.

## 🛠 Teknolojiler

Bu proje aşağıdaki teknolojiler kullanılarak geliştirilmiştir:

- **Backend**: .NET 9.0, C#, ASP.NET Core Web API
- **Frontend**: React.js, TypeScript, Vite
- **Yapay Zeka**: Google Gemini API, RAG (Retrieval-Augmented Generation) mimarisi
- **Diğer Araçlar**: In-Memory Vector Store, Entity Framework Core, Google OAuth (Kimlik Doğrulama)

## ⚙️ Kurulum

Projeyi kendi bilgisayarınızda çalıştırmak için aşağıdaki adımları izleyin:

1. Projeyi klonlayın:
   ```bash
   git clone https://github.com/Sattuf/ParseAI-.git
   ```

2. Proje dizinine gidin:
   ```bash
   cd ParseAI-
   ```

3. Backend'i çalıştırın:
   ```bash
   cd src/AnalyzeChat.API
   # Not: appsettings.Development.json dosyasını oluşturup API anahtarlarınızı eklemeyi unutmayın
   dotnet run
   ```

4. Frontend'i çalıştırın (yeni bir terminalde):
   ```bash
   cd ../../frontend
   npm install
   npm run dev
   ```

## 🚀 Kullanım

Projeyi kurduktan sonra:
1. Tarayıcınızda `http://localhost:5173` (veya Vite'ın verdiği port) adresine gidin.
2. Sisteme giriş yapın.
3. Analiz etmek istediğiniz PDF dosyalarını yükleyin.
4. Sohbet arayüzünü kullanarak belgeleriniz hakkında sorular sorun ve yapay zekanın belgeye dayalı yanıtlar vermesini izleyin!

## 🤝 Katkıda Bulunma

Katkılarınız projenin gelişimi için çok önemlidir! Katkıda bulunmak isterseniz lütfen şu adımları izleyin:

1. Bu depoyu (repository) Fork'layın.
2. Yeni bir özellik dalı oluşturun (`git checkout -b ozellik/HarikaBirOzellik`).
3. Yaptığınız değişiklikleri kaydedin (`git commit -m 'Yeni harika bir özellik eklendi'`).
4. Değişiklikleri kendi dalınıza gönderin (`git push origin ozellik/HarikaBirOzellik`).
5. GitHub üzerinden bir Pull Request (Çekme İsteği) oluşturun.

## 📄 Lisans

Bu proje **MIT Lisansı** altında lisanslanmıştır. Detaylar için `LICENSE` dosyasına bakabilirsiniz.

## 📬 İletişim

Geliştirici: Sattuf - [GitHub Profilim](https://github.com/Sattuf)

Proje Bağlantısı: [https://github.com/Sattuf/ParseAI-](https://github.com/Sattuf/ParseAI-)
