using FluentValidation;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Restaurantes;
using Restaurante.Application.Features.Restaurantes.CrearRestaurante;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Usuarios;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Application.Tests.Features.Restaurantes;

public class CrearRestauranteTests
{
    private readonly RestauranteRepositoryEnMemoria _restaurantes = new();
    private readonly UsuarioRepositoryEnMemoria _usuarios = new();
    private readonly HasherFalso _hasher = new();
    private readonly RelojFijo _reloj = new();

    private CrearRestauranteHandler CrearHandler() => new(_restaurantes, _usuarios, _hasher, _reloj);

    private static CrearRestauranteCommand Comando(
        string nombre = "La Esquina", string slug = "la-esquina", PaletaDto? paleta = null,
        string usuario = "admin.esquina", string contrasena = "secreta123") =>
        new(nombre, slug, paleta, new AdministradorInicialDto(usuario, contrasena));

    [Fact(DisplayName = "002/RF-01: crea el restaurante activo y su administrador general en un solo guardado")]
    public async Task CreaRestauranteYAdministradorEnUnGuardado()
    {
        var dto = await CrearHandler().Handle(Comando(), CancellationToken.None);

        var restaurante = Assert.Single(_restaurantes.Restaurantes);
        var admin = Assert.Single(_usuarios.Usuarios);
        Assert.True(restaurante.Activo);
        Assert.Equal(Rol.AdministradorGeneral, admin.Rol);
        Assert.Equal(restaurante.Id, admin.RestauranteId);
        Assert.Equal("hash:secreta123", admin.HashContrasena);
        Assert.Equal(1, _restaurantes.GuardadosLlamados);
        Assert.Equal(0, _usuarios.GuardadosLlamados);
        Assert.Equal(RestauranteDto.Desde(restaurante), dto);
        Assert.Equal("la-esquina", dto.Slug);
    }

    [Fact(DisplayName = "002/RF-01: sin paleta el restaurante queda con la paleta vacía y con paleta la conserva")]
    public async Task ConservaLaPaleta()
    {
        var dto = await CrearHandler().Handle(
            Comando(paleta: new PaletaDto("#112233", null, null, null, "#ABCDEF")), CancellationToken.None);

        Assert.Equal(new PaletaDto("#112233", null, null, null, "#ABCDEF"), dto.Paleta);
    }

    [Fact(DisplayName = "002/RF-02: el nombre repetido lanza ConflictoException")]
    public async Task NombreRepetido()
    {
        _restaurantes.Agregar(RestauranteEntidad.Crear("La Esquina", "otro", Restaurante.Domain.Restaurantes.Paleta.Vacia, _reloj.Ahora));

        var ex = await Assert.ThrowsAsync<ConflictoException>(() =>
            CrearHandler().Handle(Comando(), CancellationToken.None));

        Assert.Equal("Ya existe un restaurante con ese nombre", ex.Message);
        Assert.Equal(0, _restaurantes.GuardadosLlamados);
    }

    [Fact(DisplayName = "002/RF-02: la unicidad del nombre distingue mayúsculas, como el índice de la base")]
    public async Task NombreDistintoSoloEnMayusculasNoEsDuplicado()
    {
        _restaurantes.Agregar(RestauranteEntidad.Crear("la esquina", "otro", Restaurante.Domain.Restaurantes.Paleta.Vacia, _reloj.Ahora));

        await CrearHandler().Handle(Comando(), CancellationToken.None);

        Assert.Equal(2, _restaurantes.Restaurantes.Count);
    }

    [Fact(DisplayName = "002/RF-02: el slug repetido lanza ConflictoException")]
    public async Task SlugRepetido()
    {
        _restaurantes.Agregar(RestauranteEntidad.Crear("Otro", "la-esquina", Restaurante.Domain.Restaurantes.Paleta.Vacia, _reloj.Ahora));

        var ex = await Assert.ThrowsAsync<ConflictoException>(() =>
            CrearHandler().Handle(Comando(), CancellationToken.None));

        Assert.Equal("Ya existe un restaurante con ese slug", ex.Message);
    }

    [Fact(DisplayName = "002/RF-02: el nombre se recorta y '  La Esquina ' duplica 'La Esquina'")]
    public async Task NombreRecortadoDuplica()
    {
        await CrearHandler().Handle(Comando(), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictoException>(() =>
            CrearHandler().Handle(Comando(nombre: "  La Esquina ", slug: "otro-slug"), CancellationToken.None));

        Assert.Equal("La Esquina", Assert.Single(_restaurantes.Restaurantes).Nombre);
    }

    [Theory(DisplayName = "002/RF-02: el validador rechaza nombre, slug, colores y administrador inválidos")]
    [InlineData("", "la-esquina", null, "admin", "secreta123", "Nombre", "El nombre es obligatorio")]
    [InlineData("   ", "la-esquina", null, "admin", "secreta123", "Nombre", "El nombre es obligatorio")]
    [InlineData("x", "ab", null, "admin", "secreta123", "Slug", "El slug debe tener entre 3 y 40 caracteres: minúsculas, números y guiones")]
    [InlineData("x", "Mayus", null, "admin", "secreta123", "Slug", "El slug debe tener entre 3 y 40 caracteres: minúsculas, números y guiones")]
    [InlineData("x", "abc\n", null, "admin", "secreta123", "Slug", "El slug debe tener entre 3 y 40 caracteres: minúsculas, números y guiones")]
    [InlineData("x", "la-esquina", null, "ab", "secreta123", "Administrador.NombreUsuario",
        "El nombre de usuario debe tener entre 3 y 50 caracteres: letras, números, punto, guion y guion bajo")]
    [InlineData("x", "la-esquina", null, "admin", "corta", "Administrador.Contrasena", "La contraseña debe tener al menos 8 caracteres")]
    public void ValidadorRechaza(string nombre, string slug, PaletaDto? paleta, string usuario, string contrasena, string campo, string mensaje)
    {
        var resultado = new CrearRestauranteValidator().Validate(Comando(nombre, slug, paleta, usuario, contrasena));

        Assert.Contains(resultado.Errors, e => e.PropertyName == campo && e.ErrorMessage == mensaje);
    }

    [Fact(DisplayName = "002/RF-02: el validador rechaza un nombre de más de 80 caracteres tras recortar")]
    public void ValidadorRechazaNombreLargo()
    {
        var validador = new CrearRestauranteValidator();

        Assert.Contains(validador.Validate(Comando(nombre: new string('a', 81))).Errors,
            e => e.PropertyName == "Nombre" && e.ErrorMessage == "El nombre no puede superar 80 caracteres");
        Assert.True(validador.Validate(Comando(nombre: " " + new string('a', 80) + " ")).IsValid);
    }

    [Fact(DisplayName = "002/RF-02: el validador rechaza un color con formato inválido en la paleta")]
    public void ValidadorRechazaColor()
    {
        var resultado = new CrearRestauranteValidator().Validate(
            Comando(paleta: new PaletaDto("rojo", null, null, null, null)));

        Assert.Contains(resultado.Errors,
            e => e.PropertyName == "Paleta.Principal" && e.ErrorMessage == "El color debe tener el formato #RRGGBB");
    }

    [Fact(DisplayName = "002/RF-02: el validador acepta una petición completa y una sin paleta")]
    public void ValidadorAcepta()
    {
        var validador = new CrearRestauranteValidator();

        Assert.True(validador.Validate(Comando()).IsValid);
        Assert.True(validador.Validate(Comando(paleta: new PaletaDto("#F97316", "#FF6600", "#111111", "#F8F9FA", "#1F2937"))).IsValid);
    }
}
