using System.Collections.Concurrent;
using Clubes.Mobile.Models;
using Microsoft.Maui.Graphics.Platform;

namespace Clubes.Mobile.Services;

/// <summary>Arquivos protegidos da API: imagens (com cache em memória), downloads autenticados e envio de fotos.</summary>
public class FileService(ClubApi api)
{
    private const int MaxCached = 40;
    private readonly ConcurrentDictionary<Guid, byte[]> _cache = new();
    private readonly ConcurrentQueue<Guid> _order = new();

    public void ClearCache()
    {
        _cache.Clear();
        while (_order.TryDequeue(out _)) { }
    }

    /// <summary>Carrega uma imagem de GET /api/arquivos/{id} (exige o cabeçalho Bearer, por isso não pode ser uma URL simples).</summary>
    public async Task<ImageSource?> GetImageAsync(Guid? fileId)
    {
        if (fileId is not { } id) return null;
        try
        {
            if (!_cache.TryGetValue(id, out var bytes))
            {
                bytes = await api.FileAsync(id);
                if (_cache.TryAdd(id, bytes))
                {
                    _order.Enqueue(id);
                    while (_order.Count > MaxCached && _order.TryDequeue(out var old)) _cache.TryRemove(old, out _);
                }
            }
            var data = bytes;
            return ImageSource.FromStream(() => new MemoryStream(data));
        }
        catch (ApiException)
        {
            return null;
        }
    }

    /// <summary>Baixa um arquivo protegido para o diretório de cache e o abre no aplicativo padrão do aparelho.</summary>
    public async Task OpenFileAsync(Guid fileId, string fileName) => await OpenBytesAsync(await api.FileAsync(fileId), fileName);

    public async Task OpenBytesAsync(byte[] content, string fileName)
    {
        var safe = string.Concat(fileName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        if (string.IsNullOrWhiteSpace(safe)) safe = "arquivo";
        var dir = Path.Combine(FileSystem.CacheDirectory, "downloads");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, safe);
        await File.WriteAllBytesAsync(path, content);
        await Launcher.Default.OpenAsync(new OpenFileRequest(safe, new ReadOnlyFile(path)));
    }

    /// <summary>Deixa o usuário tirar ou escolher uma foto e a envia. Devolve null se ele desistir.</summary>
    public async Task<FileDto?> PickAndUploadPhotoAsync()
    {
        var choice = await Dialogs.ActionSheetAsync("Foto", "Tirar foto", "Escolher da galeria");
        FileResult? photo = null;
        try
        {
            if (choice == "Tirar foto")
            {
                if (!MediaPicker.Default.IsCaptureSupported) throw new ApiException("camera", "A câmera não está disponível neste aparelho.", 0);
                photo = await MediaPicker.Default.CapturePhotoAsync();
            }
            else if (choice == "Escolher da galeria")
            {
                photo = (await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { SelectionLimit = 1 }))?.FirstOrDefault();
            }
        }
        catch (PermissionException)
        {
            throw new ApiException("permissao", "Permita o acesso à câmera e às fotos nas configurações do aparelho para enviar uma imagem.", 0);
        }
        catch (FeatureNotSupportedException)
        {
            throw new ApiException("camera", "Este aparelho não permite capturar ou escolher fotos.", 0);
        }
        if (photo is null) return null;

        await using var stream = await photo.OpenReadAsync();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        var (bytes, name, contentType) = Normalize(buffer.ToArray(), photo.FileName);
        return await api.UploadAsync(bytes, name, contentType);
    }

    /// <summary>Reduz e converte a foto para JPEG (o servidor aceita JPG, PNG e WEBP até 10 MB).</summary>
    private static (byte[] Bytes, string Name, string ContentType) Normalize(byte[] original, string fileName)
    {
        try
        {
            using var input = new MemoryStream(original);
            using var image = PlatformImage.FromStream(input);
            using var resized = image.Width > 1600 || image.Height > 1600 ? image.Downsize(1600, true) : image;
            using var output = new MemoryStream();
            resized.Save(output, ImageFormat.Jpeg, 0.85f);
            if (output.Length > 0) return (output.ToArray(), "foto.jpg", "image/jpeg");
        }
        catch (Exception)
        {
            // Plataforma sem suporte à conversão: envia o arquivo original.
        }
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var type = ext switch { ".png" => "image/png", ".webp" => "image/webp", _ => "image/jpeg" };
        if (ext is not (".png" or ".webp" or ".jpg" or ".jpeg")) fileName = Path.ChangeExtension(fileName, ".jpg");
        return (original, fileName, type);
    }
}
