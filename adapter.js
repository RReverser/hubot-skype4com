var Hubot = require('hubot'),
    NativeAdapter = require('edge').func(__dirname + '\\SkypeAdapter.dll')
;

function SkypeAdapter(robot) {
    Hubot.Adapter.call(this, robot);
    
    var adapter = this;

    NativeAdapter(null, function (error, native) {
        if (error) return robot.logger.error(error);

        Object.keys(native).forEach(function (cmd) {
            adapter[cmd] = function () {
                var args = Array.prototype.slice.call(arguments);
                native[cmd](args, function (error) {
                    if (error) return robot.logger.error(error);
                });
            };
        });

        adapter.onAttachmentStatus(function (status) {
            switch (status) {
                case 'apiAttachAvailable':
                    robot.logger.info('Skype is launched. Connecting...');
                    break;

                case 'apiAttachSuccess':
                    robot.logger.info('Connected.');
                    adapter.emit('connected');
                    break;

                case 'apiAttachNotAvailable':
                case 'apiAttachRefused':
                    robot.logger.error('Connection was lost or refused.');
                    robot.shutdown();
                    break;
            }
        });

        adapter.onMessage(function (data) {
            var user = robot.brain.userForId(data.chatId, {name: data.chatName});
            var className;
            switch (data.messageType) {
                case 'cmeSaid':
                    className = 'TextMessage';
                    break;

                case 'cmeSetTopic':
                    className = 'TopicMessage';
                    break;

                default:
                    className = 'Message';
                    break;
            }
            var message = new Hubot[className](user);
            message.text = data.messageText;
            message.id = data.messageId;
            adapter.receive(message);
        });
    });

    robot.logger.info('Activating Skype...');
}

SkypeAdapter.prototype = Object.create(Hubot.Adapter.prototype);

exports.use = function (robot) {
    return new SkypeAdapter(robot);
};
