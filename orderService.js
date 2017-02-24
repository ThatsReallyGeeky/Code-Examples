(function(){

    var app = angular.module('ots');

    app.service('OrderService', ['$http', 'Order', 'OrderItem', function($http, Order, OrderItem){

        var dateInUse = moment();
        var apiEndpoint = __env.api.order + '/orders';

        this.getItemsInRunOrder = function(items){

            var output = [];

            for(var x = 0; x < items.length; x++){

                var itemInput = items[x];
                var itemRun = parseInt(itemInput.run);
                var arrKey = itemRun;

                if(!output[arrKey]){

                    output[arrKey] = {
                        run: itemRun,
                        items: []
                    };

                }

                output[arrKey].items.push(itemInput);

            }

            output = output.filter(function(element){

                return element !== undefined;

            });

            return output;

        };

        this.getAll = function(params){

            var onSuccess = function(response){

                var loads = response.data;

                var orders = [];

                for(var i = 0; i < loads.length; i++){

                    var load = loads[i];
                    var orders = load.orders;

                    for(var x = 0; x < orders.length; x++){

                        var orderInput = load.orders[x];
                        loads[i].orders[x] = new Order(orderInput, params.stage);

                    }

                }

                return loads;

            };

            var req = {
                params: {
                    site: params.site,
                    date: dateInUse.format("YYYY-MM-DD"),
                    stage: params.stage
                }
            };

            return $http.get(apiEndpoint, req).then(onSuccess);

        };

        this.getOrder = function(params){

            if(!params.orderNumber){

                return;

            }

            var onSuccess = function(response){

                var data = response.data;

                var orders = [];

                for(var key in data)
                {
                    orders.push(new Order(data[key], params.stage));

                }

                return orders;

            };

            return $http.get(apiEndpoint + "/" + params.orderNumber, {params:{stage:params.stage, withItems:1}}).then(onSuccess);

        };

        this.getAllItems = function(params){

            var onSuccess = function(response){

                var items = response.data.items, output = [];

                for(var x = 0; x < items.length; x++){

                    output.push(new OrderItem(items[x]));

                }

                return output;

            };

            return $http.get(apiEndpoint + '/items', {params:params}).then(onSuccess);

        };

        this.searchByCustomerName = function(value){

            var params = {
                term: value,
                type: 'name'
            };

            return this.search(params);

        };

        this.searchByOrderId = function(value){

            var params = {
                term: value,
                type: 'order'
            };

            return this.search(params);

        };

        this.searchByPostCode = function(value){

            var params = {
                term: value,
                type: 'postcode'
            };

            return this.search(params);

        };

        this.search = function(params){

            var req = {};
            req.params = params;

            var onSuccess = function(response){

                var data = response.data;

                var orders = [];

                for(var x = 0; x < items.length; x++)
                {
                    orders.push(new Order(data[x]));

                }

                return orders;

            };

            return $http.get(apiEndpoint + '/search', req).then(onSuccess);

        };

    }]);

}());