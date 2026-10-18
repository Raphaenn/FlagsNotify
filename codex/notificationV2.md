Alternativa recomendada: REST + Push
É suficiente para a primeira versão e não exige SignalR.
O backend precisaria persistir as notificações e fornecer endpoints semelhantes a:
GET /api/v1/notifications?cursor=...&size=20
GET /api/v1/notifications/unread-count
PATCH /api/v1/notifications/{notificationId}/read
PATCH /api/v1/notifications/read-all
DELETE /api/v1/notifications/{notificationId}
No app, um hook React Query administraria:
useNotifications()
useUnreadNotificationCount()
useMarkNotificationAsRead()
useMarkAllNotificationsAsRead()
useDeleteNotification()
A lista seria atualizada quando:
- o usuário abre o modal;
- o app volta para foreground;
- o usuário faz pull-to-refresh;
- chega um push com o app aberto.
  O listener do Expo pode apenas invalidar a query:
  Notifications.addNotificationReceivedListener(() => {
  queryClient.invalidateQueries({
  queryKey: ['notifications'],
  });
  });
  Assim, o push serve como aviso de que há algo novo, e a API REST continua sendo a fonte confiável. O Expo fornece listeners para notificações recebidas e interações do usuário. Documentação do Expo.