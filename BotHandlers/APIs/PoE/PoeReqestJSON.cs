namespace BotHandlers.APIs.PoE
{
    public static class PoeReqestJSON
    {
        public static object ExaltedPriceRequest()
        {
            return new
            {
                query = new
                {
                    status = new
                    {
                        option = "online"
                    },
                    filters = new
                    {
                        trade_filters = new
                        {
                            disabled = false,
                            filters = new
                            {
                                sale_type = new
                                {
                                    option = "priced"
                                },
                                price = new
                                {
                                    option = "chaos",
                                    min = 1,
                                    max = 9999
                                }
                            }
                        }
                    },
                    type = "Exalted Orb"
                },
                sort = new
                {
                    price = "asc"
                }
            };
        }

        public static object NameAttributeRequest(string item, string links = null)
        {
            return new
            {
                query = new
                {
                    status = new
                    {
                        option = "online"
                    },
                    filters = new
                    {
                        misc_filters = new
                        {
                            disabled = false,
                            filters = new
                            {
                                corrupted = new
                                {
                                    option = false
                                }
                            }
                        },
                        socket_filters = new
                        {
                            disabled = false,
                            filters = new
                            {
                                links = new
                                {
                                    min = string.IsNullOrWhiteSpace(links) ? 0 : int.Parse(links)
                                }
                            }
                        },
                        trade_filters = new
                        {
                            disabled = false,
                            filters = new
                            {
                                sale_type = new
                                {
                                    option = "priced"
                                }
                            }
                        }
                    },
                    name = item
                },
                sort = new
                {
                    price = "asc"
                }
            };
        }

        public static object TypeAttributeRequest(string item)
        {
            return new
            {
                query = new
                {
                    status = new
                    {
                        option = "online"
                    },
                    filters = new
                    {
                        trade_filters = new
                        {
                            disabled = false,
                            filters = new
                            {
                                sale_type = new
                                {
                                    option = "priced"
                                }
                            }
                        }
                    },
                    type = item
                },
                sort = new
                {
                    price = "asc"
                }
            };
        }
    }
}
