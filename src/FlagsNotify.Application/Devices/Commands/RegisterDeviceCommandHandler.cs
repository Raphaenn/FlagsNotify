using FlagsNotify.Application.Abstractions;
using FlagsNotify.Application.DTO;
using FlagsNotify.Domain.Entities;
using FlagsNotify.Domain.Interfaces;
using FlagsNotify.Domain.ValueObjects;

namespace FlagsNotify.Application.Devices.Commands;

public sealed class RegisterDeviceCommandHandler
{
    private readonly IDeviceRepository _repository;

    public RegisterDeviceCommandHandler(IDeviceRepository repository)
    {
        _repository = repository;
    }

    public async Task<RegisterDeviceResult> HandleAsync(
        RegisterDeviceCommand command,
        CancellationToken cancellationToken)
    {
        ExpoPushToken token = ExpoPushToken.Create(command.ExpoPushToken);
        DeviceRegistration device = await _repository.RegisterAsync(
            command.UserId, 
            token,
            cancellationToken);

        return new RegisterDeviceResult(
            device.Id,
            device.UserId,
            device.Token.Value,
            device.IsActive);
    }
}
